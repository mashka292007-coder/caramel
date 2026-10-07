using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.TelegramBot
{
    public class BotSession
    {
        public long ChatId { get; set; }
        public OrderStep Step { get; set; } = OrderStep.Start;
        public int? UserId { get; set; }
        public string? TempEmail { get; set; }
        public string? TempPassword { get; set; }
        public string? CustomerFirstName { get; set; }
        public string? CustomerLastName { get; set; }
        public string? Phone { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? SelectedCategoryId { get; set; }
        public Dictionary<int, int> SelectedProducts { get; set; } = new();
        public bool IsDelivery { get; set; }
        public string? Address { get; set; }
        public string? DeliveryDate { get; set; }
        public string? DeliveryTime { get; set; }
        public int? PaymentMethodId { get; set; }
        public string? PaymentMethodName { get; set; }
        public string? Comment { get; set; }
        public int CatalogPage { get; set; } = 0;
        public int OrdersPage { get; set; } = 0;
        public int? NavMessageId { get; set; }

        public string FullName =>
            $"{CustomerFirstName} {CustomerLastName}".Trim();
    }
}
