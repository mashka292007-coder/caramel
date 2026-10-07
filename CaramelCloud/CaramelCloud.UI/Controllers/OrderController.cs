using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.Infrastructure.Services.Interfaces;
using CaramelCloud.UI.Models.Order;
using CaramelCloud.UI.Models.OrderAssignment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CaramelCloud.UI.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IOrderService _orderService;
        private readonly UserManager<User> _userManager;
        private readonly IGeocodingApiService _geocodingService;
        private readonly IDeliveryRouteApiService _routeService;
        private readonly IOrderStatusNotifier _statusNotifier;


        public OrderController(
            IUnitOfWork uow,
            IOrderService orderService,
            UserManager<User> userManager,
            IGeocodingApiService geocodingService,
            IDeliveryRouteApiService routeService, IOrderStatusNotifier statusNotifier)
        {
            _uow = uow;
            _orderService = orderService;
            _userManager = userManager;
            _geocodingService = geocodingService;
            _routeService = routeService;
            _statusNotifier = statusNotifier;
        }

        public IActionResult Index(int pageNumber = 1)
        {
            int pageSize = 20;

            IEnumerable<Order> orders;

            if (User.IsInRole("Admin"))
            {
                orders = _uow.Order.GetAll(
                    includeProperties: "Status,CustomerProfile,CustomerProfile.User,CreatedByUser");
            }
            else
            {
                var userId = GetCurrentUserId();
                var profile = _uow.CustomerProfile.Get(cp => cp.UserId == userId);

                orders = _uow.Order.GetAll(
                    filter: o =>
                        o.CreatedByUserId == userId
                        || (profile != null && o.CustomerProfileId == profile.Id),
                    includeProperties: "Status,OrderItems,OrderItems.Product,CustomerProfile,CustomerProfile.User"
                );
            }

            var mapped = orders
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderListItemViewModel
                {
                    Id = o.Id,
                    CustomerName = ResolveCustomerName(o),
                    CustomerPhone = o.CustomerPhone,
                    StatusName = o.Status?.Name ?? "—",
                    StatusId = o.StatusId,
                    TotalAmount = o.TotalAmount,
                    CreatedAt = o.CreatedAt,
                    DeliveryDate = o.DeliveryDate,
                    IsClientOrder = o.CustomerProfileId.HasValue,
                    Source = ResolveSource(o),
                })
                .ToList();

            ViewBag.TotalCount = mapped.Count;
            ViewBag.ActiveCount = mapped.Count(o => o.StatusId < 4);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling((double)mapped.Count / pageSize);

            var paged = mapped
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(paged);
        }

        public IActionResult Details(int? id)
        {
            if (id is null) return NotFound();

            var order = _uow.Order.Get(
                o => o.Id == id.Value,
                includeProperties: "Status,PaymentMethod,DeliveryMethod,DeliveryAddress," +
                                   "OrderItems,OrderItems.Product,OrderItems.CustomCake," +
                                   "CustomerProfile,CustomerProfile.User,CreatedByUser");

            if (order is null) return NotFound();

            var currentUserId = GetCurrentUserId();
            var userProfile = _uow.CustomerProfile.Get(cp => cp.UserId == currentUserId);

            bool hasAccess = User.IsInRole("Admin") ||
                             order.CreatedByUserId == currentUserId ||
                             (userProfile != null && order.CustomerProfileId == userProfile.Id);

            if (!hasAccess) return Forbid();

            var model = MapToViewModel(order);

            if (User.IsInRole("Admin"))
            {
                var assignments = _uow.OrderAssignment.GetAll(
                    filter: a => a.OrderId == order.Id,
                    includeProperties: "Employee,AssignmentType,AssignmentStatus,AssignedByUser");

                ViewBag.Assignments = assignments.Select(a => new AssignmentViewModel
                {
                    Id = a.Id,
                    OrderId = a.OrderId,
                    EmployeeName = $"{a.Employee?.FirstName} {a.Employee?.LastName}",
                    AssignmentType = a.AssignmentType?.Name ?? "—",
                    AssignmentStatus = a.AssignmentStatus?.Name ?? "—",
                    AssignmentStatusId = a.AssignmentStatusId,
                    AssignedBy = $"{a.AssignedByUser?.FirstName} {a.AssignedByUser?.LastName}",
                    Notes = a.Notes,
                    AssignedAt = a.AssignedAt,
                    CompletedAt = a.CompletedAt,
                }).ToList();

                ViewBag.OrderStatuses = _uow.OrderStatus
                    .GetAll().Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                    .ToList();
            }

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateOrderViewModel();
            PopulateAdminDropdowns(model);
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            if (!model.Items.Any(i => i.ProductId.HasValue && i.Quantity > 0))
                ModelState.AddModelError(string.Empty, "Додайте хоча б один продукт");

            if (!ModelState.IsValid)
            {
                PopulateAdminDropdowns(model);
                return View(model);
            }

            var order = new Order
            {
                CustomerProfileId = model.CustomerProfileId,
                CustomerName = model.CustomerName?.Trim(),
                CustomerPhone = model.CustomerPhone?.Trim(),
                CreatedByUserId = GetCurrentUserId(),
                StatusId = 1,
                PaymentMethodId = model.PaymentMethodId,
                DeliveryMethodId = model.DeliveryMethodId,
                DeliveryAddressId = model.DeliveryAddressId,
                DeliveryDate = model.DeliveryDate,
                Comment = model.Comment?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var items = model.Items
                .Where(i => i.ProductId.HasValue && i.Quantity > 0)
                .Select(i => (i.ProductId!.Value, i.Quantity))
                .ToList();

            var orderId = await _orderService.CreateOrderAsync(order, items);

            TempData["Success"] = "Замовлення успішно створено";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetCustomerInfo(int id)
        {
            var profile = _uow.CustomerProfile.Get(
                cp => cp.Id == id,
                includeProperties: "User");

            if (profile is null) return NotFound();

            return Json(new
            {
                name = $"{profile.User.FirstName} {profile.User.LastName}".Trim(),
                phone = profile.User.PhoneNumber ?? "",
                email = profile.User.Email ?? ""
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetProductInfo(int id)
        {
            var product = _uow.Product.Get(p => p.Id == id);

            if (product is null) return NotFound();

            return Json(new
            {
                product.Id,
                product.Name,
                product.Price,
                product.ImageUrl,
                product.IsAvailable
            });
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpPost]
        public async Task<IActionResult> CalculateDelivery([FromBody] string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return Json(new { success = false, message = "Адресу не вказано" });

            var coordinates = await _geocodingService.GetCoordinatesAsync(address);
            if (!coordinates.IsFound)
                return Json(new
                {
                    success = false,
                    message = "Адресу не знайдено. Спробуйте формат: \"місто, вулиця\" або \"місто, область\". Наприклад: \"Хмельницький, вул. Свободи 1\""
                });

            var route = await _routeService.CalculateRouteAsync(coordinates);
            if (!route.IsFound)
                return Json(new
                {
                    success = false,
                    message = "Маршрут тимчасово недоступний. Вкажіть вартість доставки вручну або зв'яжіться з нами.",
                    addressFound = true,
                    displayName = coordinates.DisplayName
                });

            return Json(new
            {
                success = true,
                displayName = coordinates.DisplayName,
                distanceKm = route.DistanceKm,
                formattedDistance = route.FormattedDistance,
                durationMinutes = route.DurationMinutes,
                formattedDuration = route.FormattedDuration,
                estimatedCost = route.EstimatedDeliveryCost,
                lat = coordinates.Latitude,
                lon = coordinates.Longitude
            });
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpGet]
        public IActionResult PlaceOrder([FromQuery] List<OrderItemFormModel> items)
        {
            var model = new CreateClientOrderViewModel();

            if (items.Any(i => i.ProductId.HasValue))
            {
                var ids = items
                    .Where(i => i.ProductId.HasValue)
                    .Select(i => i.ProductId!.Value)
                    .Distinct()
                    .ToList();

                var products = _uow.Product
                    .GetAll(p => ids.Contains(p.Id))
                    .Select(p => new { p.Id, p.Name, p.Price, p.ImageUrl })
                    .ToDictionary(p => p.Id);

                model.Items = items
                    .Where(i => i.ProductId.HasValue && products.ContainsKey(i.ProductId.Value))
                    .Select(i =>
                    {
                        var p = products[i.ProductId!.Value];
                        return new OrderItemFormModel
                        {
                            ProductId = i.ProductId,
                            Quantity = i.Quantity,
                            ProductName = p.Name,
                            UnitPrice = p.Price,
                        };
                    })
                    .ToList();

                ViewBag.ItemImages = products.ToDictionary(
                    p => p.Key,
                    p => p.Value.ImageUrl ?? "");
            }

            PopulateClientDropdowns(model);
            return View(model);
        }


        [Authorize(Roles = "Customer,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CreateClientOrderViewModel model)
        {
            if (model.DeliveryMethodId == 2 && model.DeliveryAddressId == null && string.IsNullOrWhiteSpace(model.DeliveryAddressCustom))
            {
                ModelState.AddModelError("DeliveryAddressCustom", "Будь ласка, вкажіть адресу доставки");
            }

            if (!model.Items.Any(i => i.ProductId.HasValue && i.Quantity > 0))
                ModelState.AddModelError(string.Empty, "Додайте хоча б один продукт");

            if (!ModelState.IsValid)
            {
                var ids = model.Items
                    .Where(i => i.ProductId.HasValue)
                    .Select(i => i.ProductId!.Value)
                    .Distinct()
                    .ToList();

                ViewBag.ItemImages = _uow.Product
                    .GetAll(p => ids.Contains(p.Id))
                    .ToDictionary(p => p.Id, p => p.ImageUrl ?? "");

                PopulateClientDropdowns(model);
                return View(model);
            }

            var userId = GetCurrentUserId();
            var profile = _uow.CustomerProfile.Get(cp => cp.UserId == userId);

            int? finalAddressId = model.DeliveryAddressId;

            if (model.DeliveryMethodId == 2 && finalAddressId == null && !string.IsNullOrWhiteSpace(model.DeliveryAddressCustom))
            {
                var normalizedAddress = model.DeliveryAddressCustom.Trim();

                var existingAddress = _uow.DeliveryAddress
                    .Get(a => a.UserId == userId &&
                              a.AddressLine.ToLower() == normalizedAddress.ToLower());

                if (existingAddress != null)
                {
                    finalAddressId = existingAddress.Id;
                }
                else
                {
                    var newAddress = new DeliveryAddress
                    {
                        UserId = userId,
                        City = "",
                        AddressLine = normalizedAddress,
                        IsDefault = false
                    };

                    _uow.DeliveryAddress.Add(newAddress);
                    await _uow.SaveAsync();
                    finalAddressId = newAddress.Id;
                }
            }

            var itemsTotal = model.Items.Sum(i => i.UnitPrice * i.Quantity);

            var order = new Order
            {
                CustomerProfileId = profile?.Id,
                CustomerPhone = profile?.User?.PhoneNumber,
                CreatedByUserId = userId,
                StatusId = 1,
                PaymentMethodId = model.PaymentMethodId,
                DeliveryMethodId = model.DeliveryMethodId,
                DeliveryAddressId = finalAddressId,

                Comment = model.Comment?.Trim(),

                TotalAmount = model.DeliveryCost,

                DeliveryDate = model.DeliveryDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var items = model.Items
                .Where(i => i.ProductId.HasValue && i.Quantity > 0)
                .Select(i => (i.ProductId!.Value, i.Quantity))
                .ToList();

            var orderId = await _orderService.CreateOrderAsync(order, items);

            TempData["ClearCart"] = true;
            TempData["Success"] = "Ваше замовлення успішно оформлено!";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id is null) return NotFound();

            var order = _uow.Order.Get(
                o => o.Id == id.Value,
                includeProperties: "CustomerProfile,CustomerProfile.User,DeliveryAddress");

            if (order is null) return NotFound();

            var model = new EditOrderViewModel
            {
                Id = order.Id,
                CustomerName = order.CustomerName
                                    ?? (order.CustomerProfile?.User is { } u
                                        ? $"{u.FirstName} {u.LastName}".Trim()
                                        : null),
                CustomerPhone = order.CustomerPhone
                                    ?? order.CustomerProfile?.User?.PhoneNumber,
                StatusId = order.StatusId,
                PaymentMethodId = order.PaymentMethodId,
                DeliveryMethodId = order.DeliveryMethodId,
                DeliveryAddressId = order.DeliveryAddressId,
                DeliveryDate = order.DeliveryDate,
                Comment = order.Comment,
                IsClientOrder = order.CustomerProfileId.HasValue,
            };

            PopulateEditDropdowns(model);
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(EditOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateEditDropdowns(model);
                return View(model);
            }

            var existing = _uow.Order.Get(o => o.Id == model.Id);
            if (existing is null) return NotFound();

            var order = new Order
            {
                Id = model.Id,
                CustomerName = model.CustomerName?.Trim(),
                CustomerPhone = model.CustomerPhone?.Trim(),
                StatusId = model.StatusId,
                PaymentMethodId = model.PaymentMethodId,
                DeliveryMethodId = model.DeliveryMethodId,
                DeliveryAddressId = model.DeliveryAddressId,
                DeliveryDate = model.DeliveryDate,
                Comment = model.Comment?.Trim(),
                TotalAmount = existing.TotalAmount,
            };

            _orderService.UpdateOrder(order);
            TempData["Success"] = "Замовлення оновлено";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, int statusId)
        {
            await _orderService.UpdateStatusAsync(orderId, statusId, GetCurrentUserId());

            var order = _uow.Order.Get(o => o.Id == orderId);
            var status = _uow.OrderStatus.Get(s => s.Id == statusId);

            if (order != null && status != null)
            {
                await _statusNotifier.NotifyOrderStatusChangedAsync(
                    order.CreatedByUserId,
                    orderId,
                    status.Name);
            }

            TempData["Success"] = "Статус замовлення оновлено";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id is null) return NotFound();
            var order = _uow.Order.Get(o => o.Id == id.Value,
                includeProperties: "Status,OrderItems,OrderItems.Product");
            if (order is null) return NotFound();
            return View(MapToViewModel(order));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            if (id is null) return NotFound();
            var order = _uow.Order.Get(o => o.Id == id.Value);
            if (order is null) return NotFound();

            _uow.Order.Remove(order);
            _uow.Save();

            TempData["Success"] = "Замовлення видалено";
            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId() =>
            int.Parse(_userManager.GetUserId(User)!);

        private static string ResolveCustomerName(Order o)
        {
            if (!string.IsNullOrEmpty(o.CustomerName)) return o.CustomerName;
            if (o.CustomerProfile?.User is { } u) return $"{u.FirstName} {u.LastName}".Trim();
            if (o.CreatedByUser is { } creator) return $"{creator.FirstName} {creator.LastName}".Trim();
            return "—";
        }

        private static OrderViewModel MapToViewModel(Order order) => new()
        {
            Id = order.Id,
            CustomerName = ResolveCustomerName(order),
            CustomerPhone = order.CustomerPhone ?? order.CustomerProfile?.User?.PhoneNumber,
            CustomerEmail = order.CustomerProfile?.User?.Email,
            StatusName = order.Status?.Name ?? "—",
            StatusId = order.StatusId,
            PaymentMethod = order.PaymentMethod?.Name,
            DeliveryMethod = order.DeliveryMethod?.Name,
            DeliveryAddress = order.DeliveryAddress is not null
            ? string.IsNullOrEmpty(order.DeliveryAddress.City)
                ? order.DeliveryAddress.AddressLine
                : $"{order.DeliveryAddress.City}, {order.DeliveryAddress.AddressLine}"
            : null,
            DeliveryDate = order.DeliveryDate,
            Comment = order.Comment,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            CreatedByUser = $"{order.CreatedByUser?.FirstName} {order.CreatedByUser?.LastName}",
            IsClientOrder = order.CustomerProfileId.HasValue,
            Items = order.OrderItems.Select(oi => new OrderItemViewModel
            {
                Id = oi.Id,
                Name = oi.Product?.Name ?? oi.CustomCake?.Name ?? "CustomCake",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
            }).ToList(),
        };

        private void PopulateAdminDropdowns(CreateOrderViewModel model)
        {
            model.CustomerProfiles = _uow.CustomerProfile
                .GetAll(includeProperties: "User")
                .Select(cp => new SelectListItem
                {
                    Value = cp.Id.ToString(),
                    Text = $"{cp.User.FirstName} {cp.User.LastName} ({cp.User.Email})"
                }).ToList();

            model.Products = _uow.Product
                .GetAll(p => p.IsAvailable)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Name} — {p.Price:N2} ₴"
                }).ToList();

            model.ProductImages = _uow.Product
                .GetAll(p => p.IsAvailable)
                .ToDictionary(p => p.Id.ToString(), p => p.ImageUrl ?? "");

            model.DeliveryAddresses = _uow.DeliveryAddress
                .GetAll(includeProperties: "User")
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = $"{a.City}, {a.AddressLine}"
                }).ToList();

            model.PaymentMethods = GetPaymentMethodItems();
            model.DeliveryMethods = GetDeliveryMethodItems();
        }

        private void PopulateClientDropdowns(CreateClientOrderViewModel model)
        {
            var userId = GetCurrentUserId();

            model.Products = _uow.Product
                .GetAll(p => p.IsAvailable)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Name} — {p.Price:N2} ₴"
                }).ToList();

            model.ProductImages = _uow.Product
                .GetAll(p => p.IsAvailable)
                .ToDictionary(p => p.Id.ToString(), p => p.ImageUrl ?? "");

            model.DeliveryAddresses = _uow.DeliveryAddress
                .GetAll(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = $"{a.City}, {a.AddressLine}",
                    Selected = a.IsDefault
                }).ToList();

            model.PaymentMethods = GetPaymentMethodItems();
            model.DeliveryMethods = GetDeliveryMethodItems();
        }

        private void PopulateEditDropdowns(EditOrderViewModel model)
        {
            model.Statuses = _uow.OrderStatus
                .GetAll().Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToList();

            model.DeliveryAddresses = _uow.DeliveryAddress
                .GetAll(includeProperties: "User")
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = $"{a.City}, {a.AddressLine}"
                }).ToList();

            model.PaymentMethods = GetPaymentMethodItems();
            model.DeliveryMethods = GetDeliveryMethodItems();
        }

        private List<SelectListItem> GetPaymentMethodItems() =>
            _uow.PaymentMethod.GetAll().Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name }).ToList();

        private List<SelectListItem> GetDeliveryMethodItems() =>
            _uow.DeliveryMethod.GetAll().Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name }).ToList();
        private static string ResolveSource(Order o)
        {
            if (!string.IsNullOrEmpty(o.Comment) &&
                (o.Comment.StartsWith("Telegram:") || o.Comment.Contains("Telegram бот")))
                return "telegram";
            if (o.CustomerProfileId.HasValue)
                return "site";
            return "admin";
        }
    }
}
