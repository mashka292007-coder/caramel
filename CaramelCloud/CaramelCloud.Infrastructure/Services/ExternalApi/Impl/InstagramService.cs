using Azure.Core;
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
    public class InstagramService : IInstagramService
    {
        private readonly HttpClient _http;
        private readonly string _accessToken;
        private readonly string _accountId;

        public InstagramService(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _accessToken = configuration["InstagramSettings:AccessToken"]
                ?? throw new InvalidOperationException("InstagramSettings:AccessToken не налаштовано");
            _accountId = configuration["InstagramSettings:InstagramAccountId"]
                ?? throw new InvalidOperationException("InstagramSettings:InstagramAccountId не налаштовано");
        }

        public async Task<bool> PublishPostAsync(string imageUrl, string caption)
        {
            var createUrl = $"https://graph.instagram.com/v19.0/{_accountId}/media";
            var createResp = await _http.PostAsync(createUrl, new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["image_url"] = imageUrl,
                    ["caption"] = caption,
                    ["access_token"] = _accessToken
                }));

            if (!createResp.IsSuccessStatusCode) return false;

            var createJson = await createResp.Content.ReadFromJsonAsync<JsonElement>();
            var containerId = createJson.GetProperty("id").GetString();

            if (string.IsNullOrEmpty(containerId)) return false;

            var publishUrl = $"https://graph.instagram.com/v19.0/{_accountId}/media_publish";
            var publishResp = await _http.PostAsync(publishUrl, new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["creation_id"] = containerId,
                    ["access_token"] = _accessToken
                }));

            return publishResp.IsSuccessStatusCode;
        }
    }
}
