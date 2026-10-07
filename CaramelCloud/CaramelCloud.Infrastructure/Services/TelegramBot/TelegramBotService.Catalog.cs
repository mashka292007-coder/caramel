using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CaramelCloud.Infrastructure.Services.TelegramBot
{
    public partial class TelegramBotService
    {
        private async Task ShowCategoriesAsync(long chatId, CancellationToken ct, BotSession session)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var categories = uow.Category.GetAll().ToList();
            if (!categories.Any())
            {
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Warning} Категорії ще не додані.",
                    cancellationToken: ct);
                return;
            }

            var rows = new List<InlineKeyboardButton[]>();
            for (int i = 0; i < categories.Count; i += 2)
            {
                var row = new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Cake} {categories[i].Name}", $"cat_{categories[i].Id}")
                };
                if (i + 1 < categories.Count)
                    row.Add(InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Cake} {categories[i + 1].Name}", $"cat_{categories[i + 1].Id}"));
                rows.Add(row.ToArray());
            }
            if (session.SelectedProducts.Count > 0)
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Basket} Кошик ({session.SelectedProducts.Values.Sum()})", "view_cart") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Basket} Пошук", "search") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Back} Головне меню", "main_menu") });

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Cart} *Оберіть категорію:*",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(rows),
                cancellationToken: ct);
        }

        private async Task ShowProductsByCategoryAsync(long chatId, int categoryId, int page, CancellationToken ct, BotSession session)
        {
            const int pageSize = 3;
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var allProducts = uow.Product
                .GetAll(includeProperties: "Category")
                .Where(p => p.IsAvailable && p.CategoryId == categoryId)
                .ToList();

            if (!allProducts.Any())
            {
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Warning} У цій категорії немає доступних продуктів.",
                    cancellationToken: ct);
                await ShowCategoriesAsync(chatId, ct, session);
                return;
            }

            var totalPages = (int)Math.Ceiling(allProducts.Count / (double)pageSize);
            var products = allProducts.Skip(page * pageSize).Take(pageSize).ToList();

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Cart} *{EscapeMd(allProducts[0].Category?.Name)}* — сторінка {page + 1}/{totalPages}",
                parseMode: ParseMode.Markdown, cancellationToken: ct);

            foreach (var p in products)
            {
                var caption =
                    $"*{EscapeMd(p.Name)}*\n" +
                    (string.IsNullOrEmpty(p.Description) ? "" : $"_{EscapeMd(p.Description)}_\n\n") +
                    $"{TelegramEmoji.Money} *{p.Price:N0} {TelegramEmoji.Hryvnia}*";

                var kb = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Minus}", $"rem_{p.Id}"),
                        InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Plus} Додати", $"add_{p.Id}"),
                    }
                });

                if (!string.IsNullOrEmpty(p.ImageUrl))
                    await _bot.SendPhoto(chatId, InputFile.FromUri(p.ImageUrl),
                        caption: caption, parseMode: ParseMode.Markdown,
                        replyMarkup: kb, cancellationToken: ct);
                else
                    await _bot.SendMessage(chatId, caption,
                        parseMode: ParseMode.Markdown, replyMarkup: kb, cancellationToken: ct);
            }

            var navRow = new List<InlineKeyboardButton>();
            if (page > 0)
                navRow.Add(InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.ArrowLeft} Назад", $"page_{categoryId}_{page - 1}"));
            if (page < totalPages - 1)
                navRow.Add(InlineKeyboardButton.WithCallbackData($"Далі {TelegramEmoji.ArrowRight}", $"page_{categoryId}_{page + 1}"));

            var navKb = new InlineKeyboardMarkup(new[]
            {
                navRow.Count > 0 ? navRow.ToArray() : Array.Empty<InlineKeyboardButton>(),
                new[] { InlineKeyboardButton.WithCallbackData(
                    session.SelectedProducts.Values.Sum() > 0
                        ? $"{TelegramEmoji.Basket} Кошик ({session.SelectedProducts.Values.Sum()})"
                        : $"{TelegramEmoji.Basket} Кошик", "view_cart") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Back} Категорії", "back_to_categories") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Receipt} Оформити замовлення", "checkout") },
            }.Where(r => r.Length > 0).ToArray());

            var navMsg = await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Basket} Натисніть *\"{TelegramEmoji.Plus} Додати\"* біля потрібних продуктів:",
                parseMode: ParseMode.Markdown,
                replyMarkup: navKb,
                cancellationToken: ct);

            session.NavMessageId = navMsg.Id;
        }

        private async Task SearchProductsAsync(long chatId, string query, BotSession session, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var products = uow.Product
                .GetAll(includeProperties: "Category")
                .Where(p => p.IsAvailable &&
                    (p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                     (p.Description ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)))
                .Take(5)
                .ToList();

            if (!products.Any())
            {
                var kb = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Basket} Спробувати ще", "search") },
                    new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Back} Головне меню", "main_menu") }
                });
                await _bot.SendMessage(chatId,
                    $"{TelegramEmoji.Warning} Нічого не знайдено за запитом *\"{EscapeMd(query)}\"*",
                    parseMode: ParseMode.Markdown, replyMarkup: kb, cancellationToken: ct);
                return;
            }

            await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Basket} Знайдено {products.Count} результат(ів) за *\"{EscapeMd(query)}\"*:",
                parseMode: ParseMode.Markdown, cancellationToken: ct);

            foreach (var p in products)
            {
                var caption =
                    $"*{EscapeMd(p.Name)}*\n" +
                    $"_{EscapeMd(p.Category?.Name)}_\n" +
                    (string.IsNullOrEmpty(p.Description) ? "" : $"{EscapeMd(p.Description)}\n\n") +
                    $"{TelegramEmoji.Money} *{p.Price:N0} {TelegramEmoji.Hryvnia}*";

                var kb = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Minus}", $"rem_{p.Id}"),
                        InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Plus} Додати", $"add_{p.Id}"),
                    }
                });

                if (!string.IsNullOrEmpty(p.ImageUrl))
                    await _bot.SendPhoto(chatId, InputFile.FromUri(p.ImageUrl),
                        caption: caption, parseMode: ParseMode.Markdown,
                        replyMarkup: kb, cancellationToken: ct);
                else
                    await _bot.SendMessage(chatId, caption,
                        parseMode: ParseMode.Markdown, replyMarkup: kb, cancellationToken: ct);
            }

            session.Step = OrderStep.ChoosingProduct;

            var navKb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(
                    session.SelectedProducts.Values.Sum() > 0
                        ? $"{TelegramEmoji.Basket} Кошик ({session.SelectedProducts.Values.Sum()})"
                        : $"{TelegramEmoji.Basket} Кошик", "view_cart") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Basket} Новий пошук", "search") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Receipt} Оформити замовлення", "checkout") },
                new[] { InlineKeyboardButton.WithCallbackData($"{TelegramEmoji.Back} Головне меню", "main_menu") },
            });

            var navMsg = await _bot.SendMessage(chatId,
                $"{TelegramEmoji.Basket} Додайте продукти або продовжіть:",
                replyMarkup: navKb, cancellationToken: ct);

            session.NavMessageId = navMsg.Id;
        }
      
    }
}