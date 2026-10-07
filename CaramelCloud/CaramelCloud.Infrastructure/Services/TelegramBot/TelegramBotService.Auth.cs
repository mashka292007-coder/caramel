using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using DomainUser = CaramelCloud.Domain.Entities.User;

namespace CaramelCloud.Infrastructure.Services.TelegramBot
{
    public partial class TelegramBotService
    {
        private async Task SendWelcomeAsync(long chatId, string? firstName, BotSession session, CancellationToken ct)
        {
            if (session.UserId.HasValue)
            {
                await ShowMainMenuAsync(chatId, session, ct);
                return;
            }

            session.Step = OrderStep.AskingAuthChoice;

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Key} Увійти в акаунт", "login") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Sparkles} Зареєструватись", "register") }
            });

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Wave} *Привіт, {firstName ?? "друже"}!*\n\n" +
                $"{TelegramEmoji.Cake} Ласкаво просимо до *CaramelCloud*!\n\n" +
                "Для замовлення потрібен акаунт:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }

        private async Task ShowMainMenuAsync(long chatId, BotSession session, CancellationToken ct)
        {
            session.Step = OrderStep.MainMenu;

            var cartCount = session.SelectedProducts.Values.Sum();
            var cartLabel = cartCount > 0
                ? $"{TelegramEmoji.Basket} Кошик ({cartCount})"
                : $"{TelegramEmoji.Basket} Кошик";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Cart} Зробити замовлення", "catalog") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Basket} Пошук продуктів", "search") },
                new[] { InlineKeyboardButton.WithCallbackData(cartLabel, "view_cart") }, 
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.List} Мої замовлення", "my_orders") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Person} Мій профіль", "my_profile") },
            });

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Confetti} *Головне меню*\n\n" +
                $"{TelegramEmoji.Person} {EscapeMd(session.FullName)}, обирайте що вас цікавить:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }

        private async Task RegisterUserAsync(long chatId, BotSession session, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<DomainUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            if (await userManager.FindByEmailAsync(session.TempEmail!) != null)
            {
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Warning} Акаунт з цим email вже існує.",
                    cancellationToken: ct);
                session.TempPassword = null;
                session.Step = OrderStep.LoginEmail;
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Person} Введіть ваш *email*:",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            var customerRole = await roleManager.FindByNameAsync("Customer");

            var user = new DomainUser
            {
                UserName = session.TempEmail,
                Email = session.TempEmail,
                FirstName = session.CustomerFirstName ?? "",
                LastName = session.CustomerLastName ?? "",
                PhoneNumber = session.Phone,
                RoleId = customerRole?.Id,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, session.TempPassword!);
            session.TempPassword = null;

            if (!result.Succeeded)
            {
                var errors = string.Join("\n• ", result.Errors.Select(e => e.Description));
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Cancel} Помилка реєстрації:\n• {errors}",
                    cancellationToken: ct);
                session.Step = OrderStep.RegisterEmail;
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Pen} Почнемо спочатку. Введіть *email*:",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            await userManager.AddToRoleAsync(user, "Customer");

            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            uow.CustomerProfile.Add(new CustomerProfile
            {
                UserId = user.Id,
                BirthDate = session.BirthDate,
                CreatedAt = DateTime.UtcNow
            });
            uow.Save();

            session.UserId = user.Id;
            session.TempEmail = null;
            _userChatMap[user.Id] = chatId;

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Confetti} *Реєстрація успішна!*\n\n" +
                $"Ласкаво просимо, *{EscapeMd(session.FullName)}*! {TelegramEmoji.Cake}",
                parseMode: ParseMode.Markdown, cancellationToken: ct);

            await ShowMainMenuAsync(chatId, session, ct);
        }

        private async Task LoginUserAsync(long chatId, BotSession session, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<DomainUser>>();

            var user = await userManager.FindByEmailAsync(session.TempEmail!);
            if (user == null)
            {
                session.TempPassword = null;
                session.TempEmail = null;
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Cancel} Користувача з таким email не знайдено.",
                    cancellationToken: ct);
                session.Step = OrderStep.LoginEmail;
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Person} Введіть *email*:",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            var passwordOk = await userManager.CheckPasswordAsync(user, session.TempPassword!);
            session.TempPassword = null;
            session.TempEmail = null;

            if (!passwordOk)
            {
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Cancel} Невірний пароль. Спробуйте ще раз.",
                    cancellationToken: ct);
                session.Step = OrderStep.LoginEmail;
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Person} Введіть *email*:",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            session.UserId = user.Id;
            session.CustomerFirstName = user.FirstName;
            session.CustomerLastName = user.LastName;
            session.Phone = user.PhoneNumber;
            _userChatMap[user.Id] = chatId;

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Check} *Вхід успішний!*\n" +
                $"З поверненням, *{EscapeMd(session.FullName)}*! {TelegramEmoji.Wave}",
                parseMode: ParseMode.Markdown, cancellationToken: ct);

            await ShowMainMenuAsync(chatId, session, ct);
        }
    }
}