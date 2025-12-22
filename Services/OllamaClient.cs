using MvcChatSample.Interfaces;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MvcChatSample.Services
{
    public class OllamaClient : IOllamaClient
    {
        private readonly HttpClient _http;

        public OllamaClient(HttpClient http)
        {
            _http = http;
        }

        public async IAsyncEnumerable<string> StreamAsync(
            string question,
            string context,
            [EnumeratorCancellation] CancellationToken ct)
        {
            var payload = new
            {
                model = "llama3.2:1b",
                stream = true,
                options = new
                {
                    temperature = 0.0,      // 0.0 es el valor más rápido (menos aleatoriedad)
                    num_ctx = 1024,
                    num_gpu = 99,
                    repeat_penalty = 1.0,   // Evita cálculos extra de penalización
                    num_thread = 8
                },
                prompt = $"Contexto: {context}\n\nPregunta: {question}"
            };

            // Serializamos manualmente para tener control total
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            //petición
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/generate")
            {
                Content = content
            };


            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);


            resp.EnsureSuccessStatusCode();

            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                using var json = JsonDocument.Parse(line);
                if (json.RootElement.TryGetProperty("response", out var token))
                {
                    // ENVÍO INMEDIATO: Sin búfer, sin Regex, sin esperas.
                    yield return token.GetString();
                }

                if (json.RootElement.TryGetProperty("done", out var done) && done.GetBoolean())
                    break;
            }
        }


    }
}


