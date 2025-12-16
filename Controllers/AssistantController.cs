
using MvcChatSample.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace MvcChatSample.Controllers
{
    public class AssistantController : Controller
    {
        private readonly IHttpClientFactory _factory;
        private readonly ILogger<AssistantController> _logger;
        private readonly RagService _rag;
        public AssistantController(IHttpClientFactory factory, ILogger<AssistantController> logger, RagService rag)
        { 
            _factory = factory;
            _logger = logger;
            _rag = rag; 
        }

        public record ChatMessageRequest(string Message);
        public record ChatReplyResponse(string reply, string? context);
        public record IngestRequest(string Title, string Content);



        [HttpPost("chat/stream")]
        public async Task ChatStream([FromBody] ChatMessageRequest req, CancellationToken ct)
        {
            Response.ContentType = "text/event-stream";
            // ... otros headers ...

            await foreach (var token in _rag.AskWithOllamaAsync_Stream(req.Message, 2, ct))
            {
                // ESCRIBE EL TOKEN DIRECTO
                // No agregues espacios extras aquí, Ollama ya los trae en el token
                await Response.WriteAsync($"data: {token}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
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
