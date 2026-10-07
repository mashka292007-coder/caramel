using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Impl
{
    public class FacebookService : IFacebookService
    {
        private readonly HttpClient _http;
        private readonly string _pageAccessToken;
        private readonly string _pageId;

        public FacebookService(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _pageAccessToken = configuration["FacebookSettings:PageAccessToken"]
                ?? throw new InvalidOperationException("FacebookSettings:PageAccessToken не налаштовано");
            _pageId = configuration["FacebookSettings:PageId"]
                ?? throw new InvalidOperationException("FacebookSettings:PageId не налаштовано");
        }

        public async Task<(bool Success, string ErrorMessage)> PublishPostAsync(string message, string? imageUrl = null)
        {
            string url;
            Dictionary<string, string> parameters;

            if (!string.IsNullOrEmpty(imageUrl))
            {
                url = $"https://graph.facebook.com/v19.0/{_pageId}/photos";
                parameters = new Dictionary<string, string>
                {
                    ["url"] = imageUrl,
                    ["caption"] = message,
                    ["access_token"] = _pageAccessToken
                };
            }
            else
            {
                url = $"https://graph.facebook.com/v19.0/{_pageId}/feed";
                parameters = new Dictionary<string, string>
                {
                    ["message"] = message,
                    ["published"] = "true",
                    ["access_token"] = _pageAccessToken
                };
            }

            var response = await _http.PostAsync(url, new FormUrlEncodedContent(parameters));
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, body);

            return (true, string.Empty);
        }
    }
}