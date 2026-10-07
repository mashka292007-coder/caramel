using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using CaramelCloud.Infrastructure.Data;
using CaramelCloud.Infrastructure.Repositories;
using CaramelCloud.Infrastructure.Services.ExternalApi.Impl;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.Infrastructure.Services.Impl;
using CaramelCloud.Infrastructure.Services.Interfaces;
using CaramelCloud.Infrastructure.Services.TelegramBot;
using CaramelCloud.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Globalization;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CaramelCloud API",
        Version = "v1",
        Description = "API для управління продуктами кондитерської CaramelCloud"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CaramelCloudConnection")));

builder.Services.AddMemoryCache();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.Configure<CaramelCloud.Infrastructure.Settings.CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GoogleAuth:ClientId"]!;
        options.ClientSecret = builder.Configuration["GoogleAuth:ClientSecret"]!;
        options.CallbackPath = "/signin-google";
    })
.AddFacebook(options =>
{
    options.AppId = builder.Configuration["FacebookAuth:AppId"]!;
    options.AppSecret = builder.Configuration["FacebookAuth:AppSecret"]!;
    options.Scope.Clear();
    options.Scope.Add("public_profile");
    options.Fields.Clear();
    options.Fields.Add("name");
    options.Fields.Add("first_name");
    options.Fields.Add("last_name");
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.Name = "CaramelCloud.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IImageService, CloudinaryImageService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderAssignmentService, OrderAssignmentService>();
builder.Services.AddScoped<IInventoryPurchaseService, InventoryPurchaseService>();
builder.Services.AddScoped<ICustomCakeService, CustomCakeService>();

builder.Services.AddHttpClient<IGeocodingApiService, GeocodingApiService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "CaramelCloud/1.0 (student project)");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddStandardResilienceHandler();

builder.Services.AddHttpClient<IDeliveryRouteApiService, DeliveryRouteApiService>(client =>
{
    client.BaseAddress = new Uri("https://api.openrouteservice.org/");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json, application/geo+json");
}).AddStandardResilienceHandler();

builder.Services.AddHttpClient<ITelegramNotificationService, TelegramNotificationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});


builder.Services.AddHttpClient<IInstagramService, InstagramService>();
builder.Services.AddHttpClient<IThreadsService, ThreadsService>();
builder.Services.AddSingleton<TelegramBotService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TelegramBotService>());
builder.Services.AddSingleton<IOrderStatusNotifier>(sp => sp.GetRequiredService<TelegramBotService>());

builder.Services.AddHttpClient<IFacebookService, FacebookService>();

builder.Services.Configure<GeminiSettings>(
    builder.Configuration.GetSection("GeminiSettings"));

builder.Services.AddHttpClient<GeminiService>();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(userManager, roleManager, db);
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CaramelCloud API v1");
    c.RoutePrefix = "swagger"; 
});


app.UseRouting();

var cultureInfo = new CultureInfo("uk-UA");
cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
var supportedCultures = new[] { cultureInfo };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultureInfo),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();