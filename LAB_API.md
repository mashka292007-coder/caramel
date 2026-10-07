Сценарій інтеграції
Назва: «Калькулятор доставки + миттєве сповіщення адміна»
Проблема користувача: клієнт кондитерської хоче дізнатись вартість доставки та скільки займе це годин, а також скільки км від кондитерської до нього. 
Адмін при цьому має отримати миттєве сповіщення у Telegram щойно клієнт підтвердив замовлення.

Потік даних:
1)З локальної SQL Server БД беруться продукти, замовлення, адреси (Product, Order, DeliveryAddress);
2)Клієнт вводить адресу доставки -> Nominatim повертає координати (lat/lon);
3)Координати клієнта + координати магазину передаються у OpenRouteService -> отримується реальний маршрут: відстань у км та час у хвилинах;
4)На основі відстані автоматично розраховується вартість доставки (50 ₴ базово + 15 ₴/км понад 3 км);
5)Усі дані збираються у DeliveryResultViewModel і відображаються у View з інтерактивною картою;
6)При оформленні замовлення Telegram Bot API надсилає адміну повідомлення з клієнтом, товарами, сумою та адресою доставки.

Використані зовнішні API
API                            Edpoint                                  Отримані дані           Ключ?           Кешування
Nominatim -> /search?q=...&format=json&limit=1&countrycodes=ua -> lat, lon, display_name ->  Ні   ->  24 год.
OpenRouteService -> POST /v2/directions/driving-car  ->  distance, duration -> Так -> Ні
Telegram Bot API -> POST /bot{token}/sendMessage  ->  дані для відправки сповіщення адміну(номер замовлення,ім'я та номер клієнта, товар,суму та інші)  -> Так  -> Ні

Fallback-стратегії
+ Nominatim недоступний -> CoordinatesDto.IsFound = false, калькулятор показує: "Адресу не знайдено. Спробуйте формат: "місто, область"";
+ OpenRouteService недоступний -> RouteInfoDto.IsFound = false, View показує: "Точний маршрут тимчасово недоступний. Зателефонуйте нам для уточнення деталей доставки";
+ Telegram недоступний -> catch (Exception) логується через ILogger, замовлення все одно зберігається в БД без виключення.
+ Жоден зовнішній збій не падає на користувача — застосунок залишається повністю працездатним.

Resilience (Microsoft.Extensions.Http.Resilience)
Пакет: Microsoft.Extensions.Http.Resilience v10.5.0
Nominatim та OpenRouteService зареєстровані з .AddStandardResilienceHandler(). Telegram — лише з client.Timeout, бо сповіщення є одноразовою подією і retry не потрібен.
Шар                          Поведінка        
* Timeout | Timeout10 сек — Nominatim, 15 сек — OpenRouteService
* Retry | Повтор при тимчасових 5xx помилках та таймаутах
* Circuit Breaker | Зупиняє запити до сервісу після серії невдач

Кешування
#Nominatim -> IMemoryCache, ключ nominatim:{address.toLower().trim()}, TTL = 24 год.
Обґрунтування: координати конкретної вулиці або міста не змінюються; кешування знижує навантаження на Nominatim (у них є rate limit) і пришвидшує повторні запити.
#OpenRouteService та Telegram — не кешуються: маршрут залежить від поточної дорожньої ситуації, сповіщення — одноразові події.

Власний Web API
Controller: ProductApiController → /api/products
Swagger UI: /swagger

Метод         Endpoint          Опис
GET           /api/products     Список усіх продуктів
GET        /api/products/{id}   Продукт за ID
POST       /api/products        Додати продукт
PUTc       /api/products/{id}   Оновити продукт
DELETE     /api/products/{id}   Видалити продукт
