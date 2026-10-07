using CaramelCloud.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Impl
{
    public class GeminiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public GeminiService(HttpClient http, IOptions<GeminiSettings> settings)
        {
            _http = http;
            _apiKey = settings.Value.ApiKey;
        }

        public async Task<string> AskAsync(string prompt, int maxRetries = 2)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={_apiKey}";

            var body = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var json = JsonSerializer.Serialize(body);

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                var response = await _http.PostAsync(url,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                var result = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var error))
                {
                    var msg = error.GetProperty("message").GetString() ?? "";

                    if (msg.Contains("high demand") && attempt < maxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2 * (attempt + 1)));
                        continue;
                    }

                    return $"Помилка API: {msg}";
                }

                if (root.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? "Порожня відповідь";
                }

                return "Не вдалось розібрати відповідь від ШІ";
            }

            return "Сервіс тимчасово недоступний. Спробуйте пізніше.";
        }
    }
}
