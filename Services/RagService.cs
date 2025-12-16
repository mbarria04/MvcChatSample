
using MvcChatSample.Interfaces;
using System;
using System.Buffers;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text;
using System.Text.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace MvcChatSample.Services
{

    public class RagService
    {
        private readonly KnowledgeStore _knowledgeStore;
        private readonly IOllamaClient _ollama;

        public RagService(
            KnowledgeStore knowledgeStore,
            IOllamaClient ollama)
        {
            _knowledgeStore = knowledgeStore;
            _ollama = ollama;
        }


        public async IAsyncEnumerable<string> AskWithOllamaAsync_Stream(
    string question,
    int maxBlocks,
    [EnumeratorCancellation] CancellationToken ct)
        {
            var context = Retrieve(question, maxBlocks);

            await foreach (var token in _ollama.StreamAsync(question, context, ct))
            {
                yield return token;
            }
        }



        /// <summary>
        /// Recupera bloques relevantes de los documentos en Knowledge/ basados en la consulta.
        /// - Usa front-matter (title, category, tags) para boosting.
        /// - Trabaja por bloques de sección (##/###), no por líneas sueltas.
        /// - Aplica normalización, intención y ranking simple.
        /// </summary>
        public string Retrieve(string query, int maxBlocks = 3)
        {
            var raw = _knowledgeStore.ReadAll() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return "(No hay documentos en Knowledge/)";

            // 0) Normalización básica: quitar BOM y decodificar HTML
            raw = StripBom(raw);
            raw = DecodeHtmlEntities(raw);

            // 1) Normalizar consulta (minúsculas, sin tildes)
            string q = Normalize(query ?? "");

            // 2) Intención (simple y efectiva)
            bool intentSeguridad = ContainsAny(q, new[] { "rol", "roles", "permiso", "permisos", "seguridad", "usuario", "usuarios", "perfil" });
            bool intentVentas = ContainsAny(q, new[] { "factura", "ventas", "cliente", "cobro", "emitir", "facturacion", "facturación" });
            bool intentProcedimiento = ContainsAny(q, new[] { "crear", "generar", "como", "cómo", "pasos", "hacer" });

            // 3) Separar documentos: por front-matter o por H1
            var docs = SplitDocumentsRobust(raw).ToList();
            if (docs.Count == 0) docs.Add(raw);

            var scoredBlocks = new List<(double score, string docTitle, string category, string blockTitle, string blockContent)>();

            foreach (var doc in docs)
            {
                var meta = ParseFrontMatter(doc);
                var docTitle = meta.TryGetValue("title", out var t) ? t : (ExtractH1(doc) ?? "(sin título)");
                var category = meta.TryGetValue("category", out var c) ? c.ToLowerInvariant() : InferCategory(doc);
                var tagsStr = meta.TryGetValue("tags", out var tg) ? tg : "";
                var tagsNorm = Normalize(tagsStr);

                var body = RemoveFrontMatter(doc);
                var blocks = SplitBlocks(body); // (title, content)

                foreach (var (blockTitle, blockContent) in blocks)
                {
                    var normBlockTitle = Normalize(blockTitle);
                    var normDocTitle = Normalize(docTitle);
                    var normContent = Normalize(blockContent);

                    double score = 0;
                    var qTokens = Tokenize(q);

                    foreach (var tok in qTokens)
                    {
                        // Pesar título del bloque, título del doc, tags y contenido
                        if (normBlockTitle.Contains(tok)) score += 3;
                        if (normDocTitle.Contains(tok)) score += 2;
                        if (tagsNorm.Contains(tok)) score += 2;
                        score += CountOccurrences(normContent, tok) * 1;
                    }

                    // Bonus por intención de "procedimiento" en títulos con "crear"
                    if (intentProcedimiento && (normBlockTitle.Contains("crear") || normDocTitle.Contains("crear")))
                        score += 4.0; // más fuerte

                    // Demote FAQ si intención es procedimiento
                    if (intentProcedimiento && normBlockTitle.Contains("preguntas frecuentes"))
                        score -= 3.0;  // evitar que gane

                    // Boost por categoría
                    if (intentSeguridad && category == "seguridad") score *= 1.6;
                    if (intentVentas && category == "ventas") score *= 1.6;

                    if (score <= 0) continue;

                    scoredBlocks.Add((score, docTitle, category, blockTitle, blockContent));
                }
            }

            // 4) Responder con los mejores bloques
            if (scoredBlocks.Count > 0)
            {
                var top = scoredBlocks
                    .OrderByDescending(x => x.score)
                    .Take(Math.Max(1, maxBlocks))
                    .ToList();

                var sb = new StringBuilder();
                foreach (var b in top)
                {
                    sb.AppendLine($"# {b.docTitle}  ({b.category})");
                    sb.AppendLine($"## {b.blockTitle}");
                    sb.AppendLine(b.blockContent.Trim());
                    sb.AppendLine();
                }
                return sb.ToString().Trim();
            }

            // 5) Fallback inteligente
            var fallbackDoc = PickFallbackDocument(docs, intentSeguridad ? "seguridad" : (intentVentas ? "ventas" : null));
            var metaFb = ParseFrontMatter(fallbackDoc);
            var fbTitle = metaFb.TryGetValue("title", out var ft) ? ft : (ExtractH1(fallbackDoc) ?? "(sin título)");
            var fbCategory = metaFb.TryGetValue("category", out var fc) ? fc : InferCategory(fallbackDoc);
            var fbBody = RemoveFrontMatter(fallbackDoc);
            var fbBlocksAll = SplitBlocks(fbBody);

            var prefer = intentVentas
                ? new[] { "Acceso", "Ir al módulo", "Datos del cliente", "Agregar", "Impuestos", "Confirmar y guardar" }
                : intentSeguridad
                    ? new[] { "Acceder", "Crear nuevo rol", "Asignar permisos", "Asociar usuarios", "Crear nuevo usuario" }
                    : Array.Empty<string>();

            // Unificar tipo y materializar para evitar error de condicional (List vs IOrderedEnumerable)
            var fbBlocksOrdered = (prefer.Length == 0)
                ? fbBlocksAll
                : fbBlocksAll
                    .OrderByDescending(b => prefer.Any(p => b.title.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ThenBy(b => b.title)
                    .ToList();

            var fbTop2 = fbBlocksOrdered
                .Where(b => intentProcedimiento ? !Normalize(b.title).Contains("preguntas frecuentes") : true)
                .Take(2)
                .Select(b => $"## {b.title}\n{b.content.Trim()}");

            return $"(Fallback) Basado en la documentación:\n\n# {fbTitle}  ({fbCategory})\n\n" +
                   string.Join("\n\n", fbTop2);
        }

        // ===== Helpers =====

        private static string StripBom(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var bom = "\uFEFF";
            return s.StartsWith(bom) ? s.Substring(bom.Length) : s;
        }

        private static string Normalize(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var lower = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in lower)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            var res = sb.ToString().Normalize(NormalizationForm.FormC);
            res = Regex.Replace(res, "\\s+", " ").Trim();
            return res;
        }

        private static string DecodeHtmlEntities(string s)
        {
            return s.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&amp;", "&");
        }

        private static IEnumerable<string> SplitDocumentsRobust(string content)
        {
            // Primero intenta por front-matter
            var fmSplits = Regex.Split(content, "(?m)(?=^---\\r?\\n)")
                                .Where(p => !string.IsNullOrWhiteSpace(p))
                                .ToList();
            if (fmSplits.Count > 0)
                return fmSplits;

            // Si no hay front-matter, dividir por H1
            var h1Splits = Regex.Split(content, "(?m)(?=^\\s*#\\s+)")
                                .Where(p => !string.IsNullOrWhiteSpace(p))
                                .ToList();
            return h1Splits.Count > 0 ? h1Splits : new[] { content };
        }

        private static Dictionary<string, string> ParseFrontMatter(string doc)
        {
            var dict = new Dictionary<string, string>();
            var m = Regex.Match(doc, "(?m)^---\\r?\\n([\\s\\S]*?)\\r?\\n---\\r?\\n?");
            if (m.Success)
            {
                var yaml = m.Groups[1].Value;
                foreach (var line in Regex.Split(yaml, "\\r?\\n"))
                {
                    var kv = Regex.Match(line, "^\\s*([A-Za-z_]+)\\s*:\\s*(.+)\\s*$");
                    if (kv.Success)
                    {
                        var k = kv.Groups[1].Value;
                        var v = kv.Groups[2].Value.Trim().Trim('"');
                        dict[k] = v;
                    }
                }
            }

            // Respaldo: H1 como title si falta
            if (!dict.ContainsKey("title"))
            {
                var h1 = ExtractH1(doc);
                if (!string.IsNullOrWhiteSpace(h1))
                    dict["title"] = h1!;
            }

            // Inferir category si falta
            if (!dict.ContainsKey("category"))
                dict["category"] = InferCategory(doc);

            return dict;
        }

        private static string RemoveFrontMatter(string doc)
        {
            return Regex.Replace(doc, "(?m)^---\\r?\\n[\\s\\S]*?\\r?\\n---\\r?\\n?", "", RegexOptions.Multiline);
        }

        private static string? ExtractH1(string doc)
        {
            var h1 = Regex.Match(doc, "(?m)^\\s*#\\s+(.+)$");
            return h1.Success ? h1.Groups[1].Value.Trim() : null;
        }

        private static string InferCategory(string doc)
        {
            var body = Normalize(RemoveFrontMatter(doc));
            if (ContainsAny(body, new[] { "factura", "ventas", "cliente", "cobro" })) return "ventas";
            if (ContainsAny(body, new[] { "rol", "roles", "permiso", "seguridad", "usuario" })) return "seguridad";
            return "general";
        }

        private static List<(string title, string content)> SplitBlocks(string body)
        {
            var text = body.Replace("\r\n", "\n");
            var lines = text.Split('\n');
            var blocks = new List<(string title, StringBuilder content)>();
            StringBuilder? current = null;
            string currentTitle = "(sin título)";

            foreach (var line in lines)
            {
                var h2 = Regex.Match(line, "^\\s*##\\s+(.*)$");
                var h3 = Regex.Match(line, "^\\s*###\\s+(.*)$");

                if (h2.Success || h3.Success)
                {
                    if (current != null)
                        blocks.Add((currentTitle, current));

                    currentTitle = (h2.Success ? h2.Groups[1].Value : h3.Groups[1].Value).Trim();
                    current = new StringBuilder();
                }
                else
                {
                    current ??= new StringBuilder();
                    current.AppendLine(line);
                }
            }
            if (current != null)
                blocks.Add((currentTitle, current));

            return blocks.Select(b => (b.title, b.content.ToString())).ToList();
        }

        private static bool ContainsAny(string text, IEnumerable<string> terms)
        {
            var normText = Normalize(text);
            foreach (var t in terms)
            {
                var tok = Normalize(t);
                if (normText.Contains(tok)) return true;
            }
            return false;
        }

        private static IEnumerable<string> Tokenize(string text)
        {
            return Regex.Matches(text, "[a-zA-Záéíóúñü]+")
                        .Select(m => Normalize(m.Value))
                        .Where(t => t.Length > 1)
                        .Distinct();
        }

        private static int CountOccurrences(string text, string term)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(term)) return 0;
            return Regex.Matches(text, Regex.Escape(term)).Count;
        }

        private static string PickFallbackDocument(IEnumerable<string> docs, string? preferredCategory)
        {
            if (preferredCategory != null)
            {
                foreach (var d in docs)
                {
                    var meta = ParseFrontMatter(d);
                    var cat = meta.TryGetValue("category", out var c) ? c.ToLowerInvariant() : InferCategory(d);
                    if (cat == preferredCategory) return d;
                }
            }
            return docs.FirstOrDefault() ?? string.Empty;
        }
    }
}
