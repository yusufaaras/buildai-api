using Build.AI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Build.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatApiController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private const string Endpoint = "https://ai-au1348ai018454803465.openai.azure.com/";
        private const string ModelName = "gpt-4.1";
        private const string ApiKey = "2YMLlMAczCfNKvceZLpw5Q2esPrsrO281FmMo0DhSjgA0rxdPL7YJQQJ99BDACfhMk5XJ3w3AAAAACOGqs6e";  

        public ChatApiController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("ask")]
        public async Task<ActionResult<ChatResponse>> AskAi([FromBody] ChatRequest request)
        {
            var url = $"{Endpoint}openai/deployments/{ModelName}/chat/completions?api-version=2023-12-01-preview";

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = "Sen yardımcı bir asistansın." },
                    new { role = "user", content = request.UserMessage }
                },
                max_tokens = 500,
                temperature = 0.3,
                top_p = 0.1,
                presence_penalty = 0.8,
                frequency_penalty = 0.5,
                stream = false
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", ApiKey);

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"OpenAI API hatası: {error}");
            }

            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            var reply = doc.RootElement
                           .GetProperty("choices")[0]
                           .GetProperty("message")
                           .GetProperty("content")
                           .GetString();

            return Ok(new ChatResponse { AiReply = reply });
        }
    }
}
