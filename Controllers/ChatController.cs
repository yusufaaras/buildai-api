using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly string _model = "gpt-4.1-mini";
    private readonly string _apiUrl;
    private readonly string[] _openaiApiKeys;
    private static int _chatCounter = 0;
    private static List<string> _history = new List<string>();
    private static readonly Random _random = new Random();

    public ChatController()
    {
        _apiUrl = Environment.GetEnvironmentVariable("https://ai-au1348ai018454803465.openai.azure.com/");
        _openaiApiKeys = Environment.GetEnvironmentVariable("2YMLlMAczCfNKvceZLpw5Q2esPrsrO281FmMo0DhSjgA0rxdPL7YJQQJ99BDACfhMk5XJ3w3AAAAACOGqs6e")?.Split(',') ?? Array.Empty<string>();
        Console.WriteLine(_apiUrl);
        Console.WriteLine(string.Join(", ", _openaiApiKeys));
        if (int.TryParse(Environment.GetEnvironmentVariable("NUM_THREADS"), out int numThreads))
        {
            Console.WriteLine(numThreads);
        }
        else
        {
            Console.WriteLine("NUM_THREADS environment variable not set or invalid.");
        }
    }

    [HttpPost("predict")]
    public async Task<IActionResult> Predict([FromForm] string inputs, [FromForm] double top_p, [FromForm] double temperature, HttpRequest request)
    {
        if (string.IsNullOrEmpty(_apiUrl) || !_openaiApiKeys.Any())
        {
            return StatusCode(500, "API URL or OpenAI API Keys not configured.");
        }

        var payload = new
        {
            model = _model,
            messages = new[] { new { role = "user", content = inputs } },
            temperature = temperature,
            top_p = top_p,
            n = 1,
            stream = true,
            presence_penalty = 0,
            frequency_penalty = 0
        };

        string openaiApiKey = _openaiApiKeys[_random.Next(_openaiApiKeys.Length)];
        Console.WriteLine(openaiApiKey);

        var headersDict = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openaiApiKey);
        requestMessage.Headers.Add("Headers", JsonSerializer.Serialize(headersDict));
        requestMessage.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        if (_chatCounter != 0)
        {
            var messages = _history.Select((item, index) => new { role = index % 2 == 0 ? "user" : "assistant", content = item })
                                     .ToArray();
            var userMessage = new { role = "user", content = inputs };
            var allMessages = messages.Concat(new[] { userMessage }).ToArray();

            payload = new
            {
                model = _model,
                messages = allMessages,
                temperature = temperature,
                top_p = top_p,
                n = 1,
                stream = true,
                presence_penalty = 0,
                frequency_penalty = 0
            };
            requestMessage.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        }
        else
        {
            payload = new
            {
                model = _model,
                messages = new[] { new { role = "user", content = inputs } },
                temperature = temperature,
                top_p = top_p,
                n = 1,
                stream = true,
                presence_penalty = 0,
                frequency_penalty = 0
            };
            requestMessage.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        }

        _chatCounter++;
        _history.Add(inputs);
        int tokenCounter = 0;
        string partialWords = "";
        int counter = 0;

        try
        {
            using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            string responseCode = response.ToString();
            //if (!responseCode.Contains("200"))
            //{
            //    Console.WriteLine($"Response code - {response}");
            //    return StatusCode((int)response.StatusCode, $"Sorry, hitting rate limit. Please try again later. {response}");
            //}

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    if (counter == 0)
                    {
                        counter++;
                        continue;
                    }
                    if (line.Length > 12 && line.Contains("\"content\"") && line.Substring(6).StartsWith("{") && line.Substring(6).EndsWith("}"))
                    {
                        try
                        {
                            var jsonObject = JsonDocument.Parse(line.Substring(6));
                            var content = jsonObject.RootElement.GetProperty("choices")[0].GetProperty("delta").GetProperty("content").GetString();
                            partialWords += content;
                            if (tokenCounter == 0)
                            {
                                _history.Add(" " + partialWords);
                            }
                            else
                            {
                                _history[_history.Count - 1] = partialWords;
                            }
                            tokenCounter++;
                            var chatHistory = _history.Select((item, index) => new { User = index % 2 == 0 ? item : null, Assistant = index % 2 != 0 ? item : null })
                                                     .Where(pair => pair.User != null || pair.Assistant != null)
                                                     .Select(pair => new string[] { pair.User, pair.Assistant })
                                                     .ToList();

                            // Since this is an API, we can't directly yield results like in Gradio.
                            // We would typically return a structured response here.
                            // For a streaming API, you might consider using Server-Sent Events (SSE) or WebSockets.
                            // For this basic conversion, we'll just log the intermediate output.
                            Console.WriteLine(JsonSerializer.Serialize(new { Chatbot = chatHistory, History = _history, ChatCounter = _chatCounter, Response = response.StatusCode, InteractiveInput = false, InteractiveButton = false }));
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"JSON Parsing Error: {ex.Message} - Line: {line}");
                        }
                        catch (KeyNotFoundException ex)
                        {
                            Console.WriteLine($"Key Not Found Error: {ex.Message} - Line: {line}");
                        }
                    }
                }
            }
            var finalChatHistory = _history.Select((item, index) => new { User = index % 2 == 0 ? item : null, Assistant = index % 2 != 0 ? item : null })
                                         .Where(pair => pair.User != null || pair.Assistant != null)
                                         .Select(pair => new string[] { pair.User, pair.Assistant })
                                         .ToList();

            return Ok(new { Chatbot = finalChatHistory, History = _history, ChatCounter = _chatCounter, Response = response.StatusCode, InteractiveInput = true, InteractiveButton = true });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error found: {e}");
            return StatusCode(500, new { Chatbot = _history.Select((item, index) => new string[] { index % 2 == 0 ? item : null, index % 2 != 0 ? item : null }).ToList(), History = _history, ChatCounter = _chatCounter, Response = "Error", InteractiveInput = true, InteractiveButton = true, Error = e.Message });
        }
        finally
        {
            Console.WriteLine(JsonSerializer.Serialize(new { ChatCounter = _chatCounter, Payload = payload, PartialWords = partialWords, TokenCounter = tokenCounter, Counter = counter }));
        }
    }

    [HttpPost("reset")]
    public IActionResult Reset()
    {
        return Ok(new { InputInteractive = false, ButtonInteractive = false });
    }
}