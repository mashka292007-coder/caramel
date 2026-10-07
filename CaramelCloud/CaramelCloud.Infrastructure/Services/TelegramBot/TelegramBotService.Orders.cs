using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CaramelCloud.Infrastructure.Services.TelegramBot
{
    public partial class TelegramBotService
    {
        private async Task AskDeliveryTypeAsync(long chatId, CancellationToken ct)
        {
            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Truck} Кур'єр", "delivery_courier") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Store} Самовивіз", "delivery_pickup") },
            });

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Package} *Як отримаєте замовлення?*",
                parseMode: ParseMode.Markdown, replyMarkup: kb, cancellationToken: ct);
        }

        private async Task AskDeliveryDateAsync(long chatId, CancellationToken ct)
        {
            var today = DateTime.Today;
            var rows = new List<InlineKeyboardButton[]>();

            for (int i = 1; i <= 4; i++)
            {
                var d = today.AddDays(i);
                var label = i == 1
                    ? $"{TelegramEmoji.Calendar} Завтра ({d:dd.MM})"
                    : $"{TelegramEmoji.Calendar} {d:dd.MM.yyyy}";
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(label, $"date_{d:dd.MM.yyyy}") });
            }

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Calendar} *Оберіть дату доставки*\n_або введіть вручну ДД.ММ.РРРР_:",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(rows),
                cancellationToken: ct);
        }

        private async Task AskDeliveryTimeAsync(long chatId, CancellationToken ct)
        {
            var slots = new[] { "09:00", "11:00", "13:00", "15:00", "17:00", "19:00" };
            var rows = slots
                .Select(t => new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Time} {t}", $"time_{t}") })
                .ToArray();

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Clock} *Оберіть зручний час*\n_або введіть вручну ГГ:ХХ_:",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(rows),
                cancellationToken: ct);
        }

        private async Task AskCommentAsync(long chatId, CancellationToken ct)
        {
            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Check} Без коментаря", "skip_comment") }
            });

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Comment} *Додайте коментар до замовлення*\n_або натисніть кнопку нижче:_",
                parseMode: ParseMode.Markdown, replyMarkup: kb, cancellationToken: ct);
        }

        private async Task ShowConfirmationAsync(long chatId, BotSession session, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var lines = new List<string>();
            decimal total = 0;

            foreach (var (pid, qty) in session.SelectedProducts)
            {
                var p = uow.Product.Get(x => x.Id == pid);
                if (p == null) continue;
                var sum = p.Price * qty;
                total += sum;
                lines.Add($"  + *{EscapeMd(p.Name)}* x {qty} = {sum:N0} {TelegramEmoji.Hryvnia}");
            }

            var deliveryLine = session.IsDelivery
                ? $"{TelegramEmoji.Truck} *Доставка* -> {EscapeMd(session.Address)}"
                : $"{TelegramEmoji.Store} *Самовивіз*";

            var text =
                $"{TelegramEmoji.Receipt} *Підтвердження замовлення*\n\n" +
                $"{TelegramEmoji.Person} {EscapeMd(session.FullName)}\n" +
                $"{TelegramEmoji.Phone} {EscapeMd(session.Phone)}\n\n" +
                $"{deliveryLine}\n" +
                $"{TelegramEmoji.Calendar} *Дата/час:* {session.DeliveryDate} о {session.DeliveryTime}\n" + $"{TelegramEmoji.Cash} *Оплата:* {EscapeMd(session.PaymentMethodName ?? "не обрано")}\n" +
                (string.IsNullOrEmpty(session.Comment) ? "" : $"{TelegramEmoji.Comment} *Коментар:* {EscapeMd(session.Comment)}\n") +
                $"\n{TelegramEmoji.Cart} *Склад замовлення:*\n" +
                string.Join("\n", lines) + "\n\n" +
                $"{TelegramEmoji.Money} *До сплати: {total:N0} {TelegramEmoji.Hryvnia}*";

            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Check} Підтвердити", "confirm_order") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Cancel} Скасувати", "cancel_order") },
            });

            await _bot.SendMessage(chatId, text,
                parseMode: ParseMode.Markdown, replyMarkup: kb, cancellationToken: ct);
        }

        private async Task CreateOrderAsync(long chatId, BotSession session, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            DateTime? deliveryDateTime = null;
            if (DateTime.TryParseExact(
                    $"{session.DeliveryDate} {session.DeliveryTime}",
                    "dd.MM.yyyy HH:mm",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var dt))
                deliveryDateTime = dt;

            int deliveryMethodId = session.IsDelivery ? 2 : 1;
            var customerProfile = uow.CustomerProfile.Get(p => p.UserId == session.UserId!.Value);

            var order = new Order
            {
                CustomerProfileId = customerProfile?.Id,
                CustomerName = session.FullName,
                CustomerPhone = session.Phone ?? "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                StatusId = 1,
                CreatedByUserId = session.UserId!.Value,
                DeliveryMethodId = deliveryMethodId,
                DeliveryDate = deliveryDateTime,
                PaymentMethodId = session.PaymentMethodId,
                Comment = string.IsNullOrEmpty(session.Comment)
                    ? "Замовлення через Telegram бот"
                    : $"Telegram: {session.Comment}",
                TotalAmount = session.SelectedProducts.Sum(kvp =>
                {
                    var p = uow.Product.Get(x => x.Id == kvp.Key);
                    return (p?.Price ?? 0) * kvp.Value;
                }),
                OrderItems = session.SelectedProducts
                    .Select(kvp => new OrderItem
                    {
                        ProductId = kvp.Key,
                        Quantity = kvp.Value,
                        UnitPrice = uow.Product.Get(x => x.Id == kvp.Key)?.Price ?? 0
                    }).ToList()
            };

            uow.Order.Add(order);
            uow.Save();

            try
            {
                var notif = scope.ServiceProvider.GetRequiredService<ITelegramNotificationService>();
                await notif.SendNewOrderNotificationAsync(
                    orderId: order.Id,
                    customerName: session.FullName,
                    customerPhone: session.Phone ?? "",
                    totalAmount: order.TotalAmount,
                    comment: session.Comment,
                    itemNames: session.SelectedProducts.Select(kvp =>
                    {
                        var p = uow.Product.Get(x => x.Id == kvp.Key);
                        return $"{p?.Name} ×{kvp.Value}";
                    }),
                    deliveryMethod: session.IsDelivery ? "Кур'єр" : "Самовивіз",
                    deliveryAddress: session.Address,
                    deliveryDate: deliveryDateTime,
                    isDelivery: session.IsDelivery,
                    paymentMethod: session.PaymentMethodName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не вдалось надіслати сповіщення адміну");
            }

            session.SelectedProducts.Clear();
            session.Address = null;
            session.DeliveryDate = null;
            session.DeliveryTime = null;
            session.Comment = null;
            session.PaymentMethodId = null;
            session.PaymentMethodName = null;
            session.Step = OrderStep.MainMenu;

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Confetti} *Дякуємо! Замовлення №{order.Id} прийнято!*\n\n" +
                $"{TelegramEmoji.Clock} Ми зв'яжемось з вами найближчим часом.",
                parseMode: ParseMode.Markdown, cancellationToken: ct);

            await ShowMainMenuAsync(chatId, session, ct);
        }

        private async Task ShowMyOrdersAsync(long chatId, BotSession session, CancellationToken ct)
        {
            if (!session.UserId.HasValue)
            {
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Warning} Для перегляду замовлень необхідно увійти.",
                    cancellationToken: ct);
                return;
            }

            const int pageSize = 3;
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var allOrders = uow.Order
                .GetAll(includeProperties: "Status,OrderItems")
                .Where(o => o.CreatedByUserId == session.UserId.Value)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            if (!allOrders.Any())
            {
                var kb = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Cart} Зробити перше замовлення", "catalog") },
                    new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Back} Назад", "main_menu") },
                });
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Info} У вас ще немає замовлень.",
                    replyMarkup: kb, cancellationToken: ct);
                return;
            }

            var page = session.OrdersPage;
            var totalPages = (int)Math.Ceiling(allOrders.Count / (double)pageSize);
            var orders = allOrders.Skip(page * pageSize).Take(pageSize).ToList();

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.List} *Мої замовлення* ({page * pageSize + 1}–{Math.Min((page + 1) * pageSize, allOrders.Count)} з {allOrders.Count})",
                parseMode: ParseMode.Markdown, cancellationToken: ct);

            foreach (var o in orders)
            {
                var statusEmoji = GetStatusEmoji(o.Status?.Name ?? "");
                var kb = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData(
                        $"{TelegramEmoji.Receipt} Деталі #{o.Id}", $"order_{o.Id}") }
                });

                await _bot.SendMessage(chatId,
                    $"{statusEmoji} *Замовлення #{o.Id}*\n" +
                    $"{TelegramEmoji.Calendar} {o.CreatedAt:dd.MM.yyyy}\n" +
                    $"{TelegramEmoji.Money} {o.TotalAmount:N0} {TelegramEmoji.Hryvnia}\n" +
                    $"{TelegramEmoji.Star} *{EscapeMd(o.Status?.Name ?? "Невідомо")}*",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: kb,
                    cancellationToken: ct);
            }

            var navRow = new List<InlineKeyboardButton>();
            if (page > 0)
                navRow.Add(InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.ArrowLeft} Новіші", $"orders_page_{page - 1}"));
            if (page < totalPages - 1)
                navRow.Add(InlineKeyboardButton.WithCallbackData($"Старіші {TelegramEmoji.ArrowRight}", $"orders_page_{page + 1}"));

            var navRows = new List<InlineKeyboardButton[]>();
            if (navRow.Count > 0) navRows.Add(navRow.ToArray());
            navRows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Back} Головне меню", "main_menu") });

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.List} *Навігація по замовленнях:*",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(navRows),
                cancellationToken: ct);
        }

        private async Task ShowOrderDetailsAsync(long chatId, int orderId, BotSession session, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var order = uow.Order.Get(
                o => o.Id == orderId,
                includeProperties: "Status,OrderItems,OrderItems.Product,DeliveryMethod,PaymentMethod");

            if (order == null || order.CreatedByUserId != session.UserId)
            {
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Warning} Замовлення не знайдено.",
                    cancellationToken: ct);
                return;
            }

            var lines = order.OrderItems
                .Select(i =>
                {
                    var name = i.Product?.Name
                        ?? (i.CustomCakeId.HasValue ? $"{TelegramEmoji.Cake} Мій торт (конструктор)" : "—");
                    return $"  {TelegramEmoji.Plus} *{EscapeMd(name)}* x {i.Quantity} = {i.UnitPrice * i.Quantity:N0} {TelegramEmoji.Hryvnia}";
                })
                .ToList();

            var deliveryLine = order.DeliveryMethod?.Name == "Кур'єр"
                ? $"{TelegramEmoji.Truck} Кур'єр"
                : $"{TelegramEmoji.Store} Самовивіз";

            var text =
                $"{TelegramEmoji.Receipt} *Замовлення #{order.Id}*\n\n" +
                $"{TelegramEmoji.Calendar} *Дата:* {order.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                $"{TelegramEmoji.Star} *Статус:* {EscapeMd(order.Status?.Name)}\n" +
                $"{deliveryLine}\n" +
                (order.DeliveryDate.HasValue ? $"{TelegramEmoji.Time} *Доставка:* {order.DeliveryDate:dd.MM.yyyy HH:mm}\n" : "") + $"{TelegramEmoji.Cash} *Оплата:* {EscapeMd(order.PaymentMethod?.Name ?? "не вказано")}\n" +
                (string.IsNullOrEmpty(order.Comment) ? "" : $"{TelegramEmoji.Comment} *Коментар:* {EscapeMd(order.Comment)}\n") +
                $"\n{TelegramEmoji.Cart} *Позиції:*\n" +
                string.Join("\n", lines) + "\n\n" +
                $"{TelegramEmoji.Money} *Разом: {order.TotalAmount:N0} {TelegramEmoji.Hryvnia}*";

            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Back} Мої замовлення", "my_orders") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Back} Головне меню", "main_menu") }
            });

            await _bot.SendMessage(chatId, text,
                parseMode: ParseMode.Markdown, replyMarkup: kb, cancellationToken: ct);
        }

        private async Task AskPaymentMethodAsync(long chatId, CancellationToken ct)
        {
            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Cash} Готівка", "payment_cash") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Card} Карта", "payment_card") },
            });

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Money} *Оберіть спосіб оплати:*",
                parseMode: ParseMode.Markdown,
                replyMarkup: kb,
                cancellationToken: ct);
        }
    
    private async Task ShowCartAsync(long chatId, BotSession session, CancellationToken ct)
        {
            if (session.SelectedProducts.Count == 0)
            {
                var emptyKb = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Cart} Перейти до каталогу", "catalog") },
                    new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Back} Головне меню", "main_menu") },
                });
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Basket} *Кошик порожній*\n\nДодайте товари з каталогу!",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: emptyKb,
                    cancellationToken: ct);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var lines = new List<string>();
            decimal total = 0;
            var rows = new List<InlineKeyboardButton[]>();

            foreach (var (pid, qty) in session.SelectedProducts)
            {
                var p = uow.Product.Get(x => x.Id == pid);
                if (p == null) continue;
                var sum = p.Price * qty;
                total += sum;
                lines.Add($"+ *{EscapeMd(p.Name)}* × {qty} = {sum:N0} {TelegramEmoji.Hryvnia}");
                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData( $"{TelegramEmoji.Package} {p.Name} × {qty}", $"noop")  
                });
                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Minus}", $"cart_rem_{pid}"),
                    InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Plus}", $"cart_add_{pid}"),
                    InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Trash}", $"cart_del_{pid}"),
                });
            }

            rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Cart} Додати ще", "catalog") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Receipt} Оформити замовлення ({total:N0} {TelegramEmoji.Hryvnia})", "checkout") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Back} Головне меню", "main_menu") });

            var text =
                $"{TelegramEmoji.Basket} *Ваш кошик:*\n\n" +
                string.Join("\n", lines) + "\n\n" +
                $"{TelegramEmoji.Money} *Разом: {total:N0} {TelegramEmoji.Hryvnia}*";

            await _bot.SendMessage(chatId, text,
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(rows),
                cancellationToken: ct);
        }
    }
}