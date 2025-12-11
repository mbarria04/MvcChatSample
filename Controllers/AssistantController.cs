
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace MvcChatSample.Controllers
{
    public class AssistantController : Controller
    {
        private readonly IHttpClientFactory _factory;
        private readonly ILogger<AssistantController> _logger;
        public AssistantController(IHttpClientFactory factory, ILogger<AssistantController> logger)
        { _factory = factory; _logger = logger; }

        public record ChatMessageRequest(string Message);
        public record ChatReplyResponse(string reply, string? context);
        public record IngestRequest(string Title, string Content);

        [HttpPost]
        [Route("assistant/chat")]
        public async Task<IActionResult> Chat([FromBody] ChatMessageRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Message)) return BadRequest(new { error = "Mensaje vacío" });
            var client = _factory.CreateClient("AssistantApi");
            var resp = await client.PostAsJsonAsync("/api/assistant/chat", req);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogWarning("Assistant API error {Status}: {Body}", resp.StatusCode, body);
                return StatusCode((int)resp.StatusCode, new { error = "Falló la consulta al asistente" });
            }
            var data = await resp.Content.ReadFromJsonAsync<ChatReplyResponse>();
            return Ok(data);
        }


        [HttpPost]
        [Route("assistant/ingest")]
        public async Task<IActionResult> Ingest([FromBody] IngestRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Title) || string.IsNullOrWhiteSpace(req?.Content)) return BadRequest(new { error = "Debe enviar Title y Content" });
            var client = _factory.CreateClient("AssistantApi");
            var resp = await client.PostAsJsonAsync("/api/assistant/ingest", req);
            return StatusCode((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
        }
    }
}
