using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Impl
{
    public class TelegramNotificationService : ITelegramNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramNotificationService> _logger;
        private readonly string? _botToken;
        private readonly string? _adminChatId;

        public TelegramNotificationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<TelegramNotificationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _botToken = configuration["Telegram:BotToken"];
            _adminChatId = configuration["Telegram:AdminChatId"];
        }

        public async Task SendNewOrderNotificationAsync(
            int orderId,
            string customerName,
            string customerPhone,
            decimal totalAmount,
            string? comment,
            IEnumerable<string> itemNames,
            string? deliveryMethod = null,
            string? deliveryAddress = null,
            DateTime? deliveryDate = null,
            bool isDelivery = false,
            string? paymentMethod = null)
        {
            if (string.IsNullOrWhiteSpace(_botToken) || string.IsNullOrWhiteSpace(_adminChatId))
            {
                _logger.LogWarning("Telegram не налаштовано — сповіщення не надіслано для замовлення #{OrderId}", orderId);
                return;
            }

            try
            {
                var items = string.Join("\n", itemNames.Select(i => $"  + {i}"));
                var deliveryLine = BuildDeliveryLine(isDelivery, deliveryMethod, deliveryAddress, deliveryDate);

                var message = $"""
                    {TelegramEmoji.Cake} *Нове замовлення #{orderId}*

                    {TelegramEmoji.Person} *Клієнт:* {EscapeMarkdown(customerName)}
                    {TelegramEmoji.Phone} *Телефон:* {EscapeMarkdown(customerPhone)}
                    {TelegramEmoji.Money} *Сума:* {totalAmount:N0} {TelegramEmoji.Hryvnia}
                    {TelegramEmoji.Cash} *Оплата:* {EscapeMarkdown(paymentMethod ?? "не вказано")}
                    {TelegramEmoji.Cart} *Позиції:*
                    {items}

                    {deliveryLine}
                    {(string.IsNullOrWhiteSpace(comment) ? "" : $"{TelegramEmoji.Comment} *Коментар:* {EscapeMarkdown(comment)}")}

                    {TelegramEmoji.Clock} {DateTime.Now:dd.MM.yyyy HH:mm}
                    """;

                await SendMessageAsync(_adminChatId, message.Trim());
                _logger.LogInformation("Telegram сповіщення надіслано адміну для замовлення #{OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не вдалось надіслати Telegram сповіщення для замовлення #{OrderId}", orderId);
            }
        }

        private async Task SendMessageAsync(string chatId, string text)
        {
            var payload = new
            {
                chat_id = chatId,
                text = text,
                parse_mode = "Markdown"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Telegram повернув помилку для chat_id={ChatId}: {Error}", chatId, errorBody);
            }
        }

        private static string BuildDeliveryLine(
             bool isDelivery,
             string? deliveryMethod,
             string? deliveryAddress,
             DateTime? deliveryDate)
        {
            var sb = new StringBuilder();

            sb.AppendLine(isDelivery
                ? $"{TelegramEmoji.Truck} *Тип:* Доставка"
                : $"{TelegramEmoji.Store} *Тип:* Самовивіз");

            if (!string.IsNullOrWhiteSpace(deliveryMethod))
                sb.AppendLine($"{TelegramEmoji.Package} *Спосіб:* {EscapeMarkdown(deliveryMethod)}");

            if (!string.IsNullOrWhiteSpace(deliveryAddress))
                sb.AppendLine($"{TelegramEmoji.Location} *Адреса:* {EscapeMarkdown(deliveryAddress)}");

            if (deliveryDate.HasValue)
                sb.AppendLine($"{TelegramEmoji.Calendar} *Дата:* {deliveryDate.Value:dd.MM.yyyy HH:mm}");

            return sb.ToString().TrimEnd();
        }

        private static string EscapeMarkdown(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "—";
            return text
                .Replace("_", "\\_")
                .Replace("*", "\\*")
                .Replace("[", "\\[")
                .Replace("`", "\\`");
        }

    }
}
