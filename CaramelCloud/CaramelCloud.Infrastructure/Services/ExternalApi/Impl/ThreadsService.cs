using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Impl
{
    public class ThreadsService : IThreadsService
    {
        private readonly HttpClient _http;
        private readonly string _accessToken;
        private readonly string _userId;

        public ThreadsService(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _accessToken = configuration["ThreadsSettings:AccessToken"]
                ?? throw new InvalidOperationException("ThreadsSettings:AccessToken не налаштовано");
            _userId = configuration["ThreadsSettings:ThreadsUserId"]
                ?? throw new InvalidOperationException("ThreadsSettings:ThreadsUserId не налаштовано");
        }

        public async Task<(bool Success, string ErrorMessage)> PublishPostAsync(string text, string? imageUrl = null)
        {
            var createUrl = $"https://graph.threads.net/v1.0/{_userId}/threads";

            var parameters = new Dictionary<string, string>
            {
                ["text"] = text,
                ["access_token"] = _accessToken
            };

            if (!string.IsNullOrEmpty(imageUrl))
            {
                parameters["media_type"] = "IMAGE";
                parameters["image_url"] = imageUrl;
            }
            else
            {
                parameters["media_type"] = "TEXT";
            }

            var createResp = await _http.PostAsync(createUrl, new FormUrlEncodedContent(parameters));
            var createBody = await createResp.Content.ReadAsStringAsync();

            if (!createResp.IsSuccessStatusCode)
                return (false, createBody);

            var createJson = JsonSerializer.Deserialize<JsonElement>(createBody);
            var containerId = createJson.GetProperty("id").GetString();

            if (string.IsNullOrEmpty(containerId))
                return (false, "Не вдалось отримати container ID");

            var publishUrl = $"https://graph.threads.net/v1.0/{_userId}/threads_publish";
            var publishResp = await _http.PostAsync(publishUrl,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["creation_id"] = containerId,
                    ["access_token"] = _accessToken
                }));

            var publishBody = await publishResp.Content.ReadAsStringAsync();

            if (!publishResp.IsSuccessStatusCode)
                return (false, publishBody);

            return (true, string.Empty);
        }
    }
}
