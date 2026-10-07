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
        private async Task ShowProfileAsync(long chatId, BotSession session, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var profile = uow.CustomerProfile.Get(p => p.UserId == session.UserId);
            var birthDate = profile?.BirthDate.HasValue == true
                ? profile.BirthDate.Value.ToString("dd.MM.yyyy")
                : "не вказано";

            var text =
                $"{TelegramEmoji.Person} *Мій профіль*\n\n" +
                $"*Ім'я:* {EscapeMd(session.CustomerFirstName)}\n" +
                $"*Прізвище:* {EscapeMd(session.CustomerLastName)}\n" +
                $"{TelegramEmoji.Phone} *Телефон:* {EscapeMd(session.Phone)}\n" +
                $"{TelegramEmoji.Calendar} *Дата народження:* {birthDate}";

            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Pen} Змінити ім'я", "edit_firstname") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Pen} Змінити прізвище", "edit_lastname") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Pen} Змінити телефон", "edit_phone") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Pen} Змінити дату народження", "edit_birthdate") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Back} Головне меню", "main_menu") },
            });

            await _bot.SendMessage(chatId, text,
                parseMode: ParseMode.Markdown,
                replyMarkup: kb,
                cancellationToken: ct);
        }

        private async Task UpdateProfileFieldAsync(long chatId, BotSession session, string field, string value, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<DomainUser>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var user = await userManager.FindByIdAsync(session.UserId!.Value.ToString());
            if (user == null) return;

            switch (field)
            {
                case "FirstName":
                    user.FirstName = value;
                    await userManager.UpdateAsync(user);
                    var ordersF = uow.Order
                        .GetAll(o => o.CreatedByUserId == session.UserId!.Value)
                        .ToList();
                    foreach (var o in ordersF)
                    {
                        o.CustomerName = $"{value} {session.CustomerLastName}".Trim();
                        uow.Order.Update(o);
                    }
                    uow.Save();
                    session.CustomerFirstName = value;
                    break;

                case "LastName":
                    user.LastName = value;
                    await userManager.UpdateAsync(user);
                    var ordersL = uow.Order
                        .GetAll(o => o.CreatedByUserId == session.UserId!.Value)
                        .ToList();
                    foreach (var o in ordersL)
                    {
                        o.CustomerName = $"{session.CustomerFirstName} {value}".Trim();
                        uow.Order.Update(o);
                    }
                    uow.Save();
                    session.CustomerLastName = value;
                    break;

                case "Phone":
                    user.PhoneNumber = value;
                    await userManager.UpdateAsync(user);
                    var ordersP = uow.Order
                        .GetAll(o => o.CreatedByUserId == session.UserId!.Value)
                        .ToList();
                    foreach (var o in ordersP)
                    {
                        o.CustomerPhone = value;
                        uow.Order.Update(o);
                    }
                    uow.Save();
                    session.Phone = value;
                    break;

                case "BirthDate":
                    var existingProfile = uow.CustomerProfile.Get(p => p.UserId == session.UserId);
                    if (existingProfile != null)
                    {
                        existingProfile.BirthDate = session.BirthDate;
                        uow.CustomerProfile.Update(existingProfile);
                        uow.Save();
                    }
                    else
                    {
                        uow.CustomerProfile.Add(new CustomerProfile
                        {
                            UserId = session.UserId!.Value,
                            BirthDate = session.BirthDate,
                            CreatedAt = DateTime.UtcNow
                        });
                        uow.Save();
                    }
                    break;
            }

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Check} Дані успішно оновлено!",
                cancellationToken: ct);

            session.Step = OrderStep.MainMenu;
            await ShowProfileAsync(chatId, session, ct);
        }


    }
}