using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.Infrastructure.Services.Interfaces;
using CaramelCloud.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using DomainUser = CaramelCloud.Domain.Entities.User;

namespace CaramelCloud.Infrastructure.Services.TelegramBot
{
    public partial class TelegramBotService : BackgroundService, IOrderStatusNotifier
    {
        private readonly ITelegramBotClient _bot;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly Dictionary<long, BotSession> _sessions = new();
        private readonly Dictionary<int, long> _userChatMap = new();

        public TelegramBotService(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            ILogger<TelegramBotService> logger)
        {
            var token = configuration["TelegramBot:Token"]
                ?? throw new InvalidOperationException("TelegramBot:Token не налаштовано");
            _bot = new TelegramBotClient(token);
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task NotifyOrderStatusChangedAsync(int userId, int orderId, string newStatus)
        {
            if (!_userChatMap.TryGetValue(userId, out var chatId)) return;
            var statusEmoji = GetStatusEmoji(newStatus);
            try
            {
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Sparkles} *Статус вашого замовлення змінено!*\n\n" +
                    $"{TelegramEmoji.Receipt} *Замовлення #{orderId}*\n" +
                    $"{statusEmoji} *Новий статус:* {newStatus}\n\n" +
                    $"_{TelegramEmoji.Cake} Дякуємо що обрали CaramelCloud!_",
                    parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не вдалось надіслати сповіщення користувачу {UserId}", userId);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var options = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
            };
            _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, options, stoppingToken);
            _logger.LogInformation("CaramelCloud Telegram Bot запущено");
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            try
            {
                if (update.Message is { } msg)
                    await HandleMessageAsync(msg, ct);
                else if (update.CallbackQuery is { } cb)
                    await HandleCallbackAsync(cb, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка обробки оновлення");
            }
        }

        private async Task HandleMessageAsync(Message message, CancellationToken ct)
        {
            var chatId = message.Chat.Id;
            var text = message.Text?.Trim() ?? "";
            var session = GetOrCreateSession(chatId);

            if (text == "/start")
            {
                await SendWelcomeAsync(chatId, message.Chat.FirstName, session, ct);
                return;
            }

            if (text.StartsWith("/search") || (session.Step == OrderStep.SearchingProduct && !string.IsNullOrEmpty(text)))
            {
                var query = text.StartsWith("/search") ? text[7..].Trim() : text;
                if (!string.IsNullOrEmpty(query))
                {
                    await SearchProductsAsync(chatId, query, session, ct);
                    return;
                }
            }

            if (!session.UserId.HasValue &&
                session.Step != OrderStep.LoginEmail &&
                session.Step != OrderStep.LoginPassword &&
                session.Step != OrderStep.RegisterEmail &&
                session.Step != OrderStep.RegisterPassword &&
                session.Step != OrderStep.RegisterFirstName &&
                session.Step != OrderStep.RegisterLastName &&
                session.Step != OrderStep.RegisterPhone &&
                session.Step != OrderStep.AskingAuthChoice)
            {
                session.Step = OrderStep.Start;
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Key} Увійти в акаунт", "login") },
                    new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Sparkles} Зареєструватись", "register") }
                });
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Warning} Сесія завершена. Оберіть дію:",
                    replyMarkup: keyboard, cancellationToken: ct);
                return;
            }

            switch (session.Step)
            {
                case OrderStep.RegisterEmail:
                    session.TempEmail = text;
                    session.Step = OrderStep.RegisterPassword;
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Lock} Придумайте пароль\n_(мін. 6 символів, має містити цифру)_:",
                        parseMode: ParseMode.Markdown, cancellationToken: ct);
                    break;

                case OrderStep.RegisterPassword:
                    try { await _bot.DeleteMessage(chatId, message.MessageId, ct); } catch { }
                    session.TempPassword = text;
                    session.Step = OrderStep.RegisterFirstName;
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Person} Введіть ваше *ім'я*:",
                        parseMode: ParseMode.Markdown, cancellationToken: ct);
                    break;

                case OrderStep.RegisterFirstName:
                    session.CustomerFirstName = text;
                    session.Step = OrderStep.RegisterLastName;
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Person} Введіть ваше *прізвище*:",
                        parseMode: ParseMode.Markdown, cancellationToken: ct);
                    break;

                case OrderStep.RegisterLastName:
                    session.CustomerLastName = text;
                    session.Step = OrderStep.RegisterPhone;
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Phone} Введіть ваш *номер телефону*:",
                        parseMode: ParseMode.Markdown, cancellationToken: ct);
                    break;

                case OrderStep.RegisterPhone:
                    var normalizedPhone = text.Trim().Replace(" ", "").Replace("-", "");
                    if (normalizedPhone.StartsWith("0") && normalizedPhone.Length == 10)
                        normalizedPhone = "+38" + normalizedPhone;
                    else if (normalizedPhone.StartsWith("380") && normalizedPhone.Length == 12)
                        normalizedPhone = "+" + normalizedPhone;
                    if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedPhone, @"^\+380\d{9}$"))
                    {
                        await _bot.SendMessage(chatId,
                            $"{TelegramEmoji.Warning} Невірний формат номера.\n\n• `+380XXXXXXXXX`\n• `0XXXXXXXXX`",
                            parseMode: ParseMode.Markdown, cancellationToken: ct);
                        break;
                    }
                    session.Phone = normalizedPhone;
                    await RegisterUserAsync(chatId, session, ct);
                    break;

                case OrderStep.LoginEmail:
                    session.TempEmail = text;
                    session.Step = OrderStep.LoginPassword;
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Lock} Введіть пароль:",
                        cancellationToken: ct);
                    break;

                case OrderStep.LoginPassword:
                    try { await _bot.DeleteMessage(chatId, message.MessageId, ct); } catch { }
                    session.TempPassword = text;
                    await LoginUserAsync(chatId, session, ct);
                    break;

                case OrderStep.SearchingProduct:
                    await SearchProductsAsync(chatId, text, session, ct);
                    break;

                case OrderStep.EnteringAddress:
                    session.Address = text;
                    session.Step = OrderStep.AskDeliveryDate;
                    await AskDeliveryDateAsync(chatId, ct);
                    break;

                case OrderStep.AskDeliveryDate:
                    if (IsValidDate(text))
                    {
                        session.DeliveryDate = text;
                        session.Step = OrderStep.AskDeliveryTime;
                        await AskDeliveryTimeAsync(chatId, ct);
                    }
                    else
                    {
                        await _bot.SendMessage(chatId,
                            $"{TelegramEmoji.Warning} Невірний формат. Введіть дату у форматі *ДД.ММ.РРРР*:",
                            parseMode: ParseMode.Markdown, cancellationToken: ct);
                    }
                    break;

                case OrderStep.AskDeliveryTime:
                    if (IsValidTime(text))
                    {
                        session.DeliveryTime = text;
                        session.Step = OrderStep.AskPaymentMethod;
                        await AskPaymentMethodAsync(chatId, ct);
                    }
                    else
                    {
                        await _bot.SendMessage(chatId,
                            $"{TelegramEmoji.Warning} Невірний формат. Введіть час у форматі *ГГ:ХХ*, наприклад `14:30`:",
                            parseMode: ParseMode.Markdown, cancellationToken: ct);
                    }
                    break;

                case OrderStep.AskComment:
                    session.Comment = text == "-" ? null : text;
                    session.Step = OrderStep.Confirm;
                    await ShowConfirmationAsync(chatId, session, ct);
                    break;

                case OrderStep.EditFirstName:
                    session.CustomerFirstName = text;
                    await UpdateProfileFieldAsync(chatId, session, "FirstName", text, ct);
                    break;

                case OrderStep.EditLastName:
                    session.CustomerLastName = text;
                    await UpdateProfileFieldAsync(chatId, session, "LastName", text, ct);
                    break;

                case OrderStep.EditPhone:
                    var normalizedEditPhone = text.Trim().Replace(" ", "").Replace("-", "");
                    if (normalizedEditPhone.StartsWith("0") && normalizedEditPhone.Length == 10)
                        normalizedEditPhone = "+38" + normalizedEditPhone;
                    else if (normalizedEditPhone.StartsWith("380") && normalizedEditPhone.Length == 12)
                        normalizedEditPhone = "+" + normalizedEditPhone;
                    if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedEditPhone, @"^\+380\d{9}$"))
                    {
                        await _bot.SendMessage(chatId,
                            $"{TelegramEmoji.Warning} Невірний формат. Введіть `+380XXXXXXXXX` або `0XXXXXXXXX`:",
                            parseMode: ParseMode.Markdown, cancellationToken: ct);
                        break;
                    }
                    session.Phone = normalizedEditPhone;
                    await UpdateProfileFieldAsync(chatId, session, "Phone", normalizedEditPhone, ct);
                    break;

                case OrderStep.EditBirthDate:
                    if (DateTime.TryParseExact(text, "dd.MM.yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var birthDate))
                    {
                        session.BirthDate = birthDate;
                        await UpdateProfileFieldAsync(chatId, session, "BirthDate", text, ct);
                    }
                    else
                    {
                        await _bot.SendMessage(chatId,
                            $"{TelegramEmoji.Warning} Невірний формат. Введіть дату народження у форматі *ДД.ММ.РРРР*:",
                            parseMode: ParseMode.Markdown, cancellationToken: ct);
                    }
                    break;
            }
        }

        private async Task HandleCallbackAsync(CallbackQuery callback, CancellationToken ct)
        {
            var chatId = callback.Message!.Chat.Id;
            var data = callback.Data ?? "";
            var session = GetOrCreateSession(chatId);

            if (!session.UserId.HasValue && data != "login" && data != "register")
            {
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Key} Увійти в акаунт", "login") },
                    new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Sparkles} Зареєструватись", "register") }
                });
                await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Warning} Сесія завершена. Оберіть дію:",
                    replyMarkup: keyboard, cancellationToken: ct);
                return;
            }

            if (data.StartsWith("cat_"))
            {
                session.SelectedCategoryId = int.Parse(data[4..]);
                session.Step = OrderStep.ChoosingProduct;
                session.CatalogPage = 0;
                await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                await ShowProductsByCategoryAsync(chatId, session.SelectedCategoryId.Value, session.CatalogPage, ct, session);
                return;
            }

            if (data.StartsWith("page_"))
            {
                var parts = data[5..].Split('_');
                session.SelectedCategoryId = int.Parse(parts[0]);
                session.CatalogPage = int.Parse(parts[1]);
                await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                await ShowProductsByCategoryAsync(chatId, session.SelectedCategoryId.Value, session.CatalogPage, ct, session);
                return;
            }

            if (data.StartsWith("cart_add_"))
            {
                var pid = int.Parse(data[9..]);
                session.SelectedProducts[pid] = session.SelectedProducts.GetValueOrDefault(pid) + 1;
                await _bot.AnswerCallbackQuery(callback.Id, $"{TelegramEmoji.Plus} Додано", cancellationToken: ct);
                await ShowCartAsync(chatId, session, ct);
                return;
            }

            if (data.StartsWith("cart_rem_"))
            {
                var pid = int.Parse(data[9..]);
                if (session.SelectedProducts.ContainsKey(pid))
                {
                    session.SelectedProducts[pid]--;
                    if (session.SelectedProducts[pid] <= 0)
                        session.SelectedProducts.Remove(pid);
                }
                await _bot.AnswerCallbackQuery(callback.Id, $"{TelegramEmoji.Minus} Прибрано", cancellationToken: ct);
                await ShowCartAsync(chatId, session, ct);
                return;
            }

            if (data.StartsWith("cart_del_"))
            {
                var pid = int.Parse(data[9..]);
                session.SelectedProducts.Remove(pid);
                await _bot.AnswerCallbackQuery(callback.Id, $"{TelegramEmoji.Trash} Видалено", cancellationToken: ct);
                await ShowCartAsync(chatId, session, ct);
                return;
            }

            if (data.StartsWith("add_"))
            {
                var pid = int.Parse(data[4..]);
                session.SelectedProducts[pid] = session.SelectedProducts.GetValueOrDefault(pid) + 1;

                var cartCount = session.SelectedProducts.Values.Sum();

                try
                {
                    var updatedKb = new InlineKeyboardMarkup(new[]
                    {
            new[]
            {
                InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Minus}", $"rem_{pid}"),
                InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Plus} Додати", $"add_{pid}"),
            }
        });
                    await _bot.EditMessageReplyMarkup(
                        chatId: callback.Message!.Chat.Id,
                        messageId: callback.Message.MessageId,
                        replyMarkup: updatedKb);
                }
                catch { }
                if (session.NavMessageId.HasValue)
                {
                    var updatedNavKb = new InlineKeyboardMarkup(new[]
                    {
            new[] { InlineKeyboardButton.WithCallbackData(
                $"{TelegramEmoji.Basket} Кошик ({cartCount})", "view_cart") },
            new[] { InlineKeyboardButton.WithCallbackData(
                $"{TelegramEmoji.Back} Категорії", "back_to_categories") },
            new[] { InlineKeyboardButton.WithCallbackData(
                $"{TelegramEmoji.Receipt} Оформити замовлення", "checkout") },
        });

                    try
                    {
                        await _bot.EditMessageReplyMarkup(
                            chatId: callback.Message!.Chat.Id,
                            messageId: session.NavMessageId.Value,
                            replyMarkup: updatedNavKb);
                    }
                    catch { }
                }

                await _bot.AnswerCallbackQuery(
                    callback.Id,
                    $"{TelegramEmoji.Check} Додано! Кошик: {cartCount} шт.",
                    cancellationToken: ct);
                return;
            }

            if (data.StartsWith("rem_"))
            {
                var pid = int.Parse(data[4..]);
                if (session.SelectedProducts.ContainsKey(pid))
                {
                    session.SelectedProducts[pid]--;
                    if (session.SelectedProducts[pid] <= 0)
                        session.SelectedProducts.Remove(pid);
                    await _bot.AnswerCallbackQuery(callback.Id, $"{TelegramEmoji.Minus} Прибрано", cancellationToken: ct);
                }
                else
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                return;
            }

            if (data.StartsWith("date_"))
            {
                session.DeliveryDate = data[5..];
                session.Step = OrderStep.AskDeliveryTime;
                await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                await AskDeliveryTimeAsync(chatId, ct);
                return;
            }

            if (data.StartsWith("time_"))
            {
                session.DeliveryTime = data[5..];
                session.Step = OrderStep.AskPaymentMethod;
                await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                await AskPaymentMethodAsync(chatId, ct);
                return;
            }

            if (data.StartsWith("payment_"))
            {
                if (data == "payment_cash")
                {
                    session.PaymentMethodId = 1; 
                    session.PaymentMethodName = "Готівка";
                }
                else if (data == "payment_card")
                {
                    session.PaymentMethodId = 2;
                    session.PaymentMethodName = "Карта";
                }
                session.Step = OrderStep.AskComment;
                await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                await AskCommentAsync(chatId, ct);
                return;
            }

            if (data.StartsWith("order_"))
            {
                var orderId = int.Parse(data[6..]);
                await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                await ShowOrderDetailsAsync(chatId, orderId, session, ct);
                return;
            }

            if (data.StartsWith("orders_page_"))
            {
                session.OrdersPage = int.Parse(data[12..]);
                await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                await ShowMyOrdersAsync(chatId, session, ct);
                return;
            }

            if (data == "noop")
            {
                await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                return;
            }

            switch (data)
            {
                case "login":
                    session.Step = OrderStep.LoginEmail;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Person} Введіть ваш *email*:",
                        parseMode: ParseMode.Markdown, cancellationToken: ct);
                    break;

                case "register":
                    session.Step = OrderStep.RegisterEmail;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Person} Введіть ваш *email*:",
                        parseMode: ParseMode.Markdown, cancellationToken: ct);
                    break;

                case "main_menu":
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await ShowMainMenuAsync(chatId, session, ct);
                    break;

                case "catalog":
                    session.Step = OrderStep.ChoosingCategory;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await ShowCategoriesAsync(chatId, ct, session);
                    break;

                case "search":
                    session.Step = OrderStep.SearchingProduct;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Basket} Введіть назву або частину назви продукту:",
                        cancellationToken: ct);
                    break;

                case "view_cart":
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await ShowCartAsync(chatId, session, ct);
                    break;

                case "my_orders":
                    session.OrdersPage = 0;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await ShowMyOrdersAsync(chatId, session, ct);
                    break;

                case "my_profile":
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await ShowProfileAsync(chatId, session, ct);
                    break;

                case "edit_firstname":
                    session.Step = OrderStep.EditFirstName;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Person} Введіть нове *ім'я*:",
                        parseMode: ParseMode.Markdown, cancellationToken: ct);
                    break;

                case "edit_lastname":
                    session.Step = OrderStep.EditLastName;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Person} Введіть нове *прізвище*:",
                        parseMode: ParseMode.Markdown, cancellationToken: ct);
                    break;

                case "edit_phone":
                    session.Step = OrderStep.EditPhone;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Phone} Введіть новий *номер телефону*:",
                        parseMode: ParseMode.Markdown, cancellationToken: ct);
                    break;

                case "edit_birthdate":
                    session.Step = OrderStep.EditBirthDate;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Calendar} Введіть *дату народження* у форматі ДД.ММ.РРРР:",
                        parseMode: ParseMode.Markdown, cancellationToken: ct);
                    break;

                case "back_to_categories":
                    session.Step = OrderStep.ChoosingCategory;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await ShowCategoriesAsync(chatId, ct, session);
                    break;

                case "checkout":
                    if (session.SelectedProducts.Count == 0)
                    {
                        await _bot.AnswerCallbackQuery(callback.Id,
                            "Кошик порожній! Додайте хоча б один продукт.",
                            showAlert: true, cancellationToken: ct);
                        return;
                    }
                    session.Step = OrderStep.AskDeliveryType;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await AskDeliveryTypeAsync(chatId, ct);
                    break;

                case "delivery_courier":
                    session.IsDelivery = true;
                    session.Step = OrderStep.EnteringAddress;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await _bot.SendMessage(chatId,
                        $"{TelegramEmoji.Location} Введіть *адресу доставки*\n_(вулиця, будинок, квартира,місто)_:",
                        parseMode: ParseMode.Markdown, cancellationToken: ct);
                    break;

                case "delivery_pickup":
                    session.IsDelivery = false;
                    session.Address = "Самовивіз";
                    session.Step = OrderStep.AskDeliveryDate;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await AskDeliveryDateAsync(chatId, ct);
                    break;

                case "skip_comment":
                    session.Comment = null;
                    session.Step = OrderStep.Confirm;
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await ShowConfirmationAsync(chatId, session, ct);
                    break;

                case "confirm_order":
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    await CreateOrderAsync(chatId, session, ct);
                    break;

                case "cancel_order":
                    session.SelectedProducts.Clear();
                    session.Address = null;
                    session.DeliveryDate = null;
                    session.DeliveryTime = null;
                    session.Comment = null;
                    session.PaymentMethodId = null;    
                    session.PaymentMethodName = null;
                    session.Step = OrderStep.MainMenu;
                    await _bot.AnswerCallbackQuery(callback.Id, "Скасовано", cancellationToken: ct);
                    await _bot.SendMessage(chatId, $"{TelegramEmoji.Cancel} Замовлення скасовано.", cancellationToken: ct);
                    await ShowMainMenuAsync(chatId, session, ct);
                    break;

                default:
                    await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                    break;
            }
        }

        private BotSession GetOrCreateSession(long chatId)
        {
            if (!_sessions.TryGetValue(chatId, out var session))
            {
                session = new BotSession { ChatId = chatId };
                _sessions[chatId] = session;
            }
            return session;
        }

        private static bool IsValidDate(string s)
            => DateTime.TryParseExact(s, "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d)
               && d >= DateTime.Today;

        private static bool IsValidTime(string s)
            => System.Text.RegularExpressions.Regex.IsMatch(s, @"^([01]\d|2[0-3]):[0-5]\d$");

        private static string EscapeMd(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "—";
            return text.Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[").Replace("`", "\\`");
        }

        private static string GetStatusEmoji(string statusName) => statusName.ToLower() switch
        {
            var s when s.Contains("нов") => TelegramEmoji.Sparkles,
            var s when s.Contains("підтв") => TelegramEmoji.Check,
            var s when s.Contains("готов") => TelegramEmoji.Cake,
            var s when s.Contains("достав") => TelegramEmoji.Truck,
            var s when s.Contains("виконан") || s.Contains("завершен") => TelegramEmoji.Check,
            var s when s.Contains("скасов") => TelegramEmoji.Cancel,
            _ => TelegramEmoji.Clock,
        };

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            _logger.LogError(ex, "Telegram Bot помилка");
            return Task.CompletedTask;
        }
    }
}