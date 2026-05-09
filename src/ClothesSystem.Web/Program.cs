using System.Threading.RateLimiting;
using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Infrastructure;
using ClothesSystem.Infrastructure.Identity;
using ClothesSystem.Infrastructure.Persistence;
using ClothesSystem.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]) &&
    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://127.0.0.1:5121");
}

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 5;
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IDataPathProvider, WebDataPathProvider>();
builder.Services.AddScoped<ILocalImageStorageService, LocalImageStorageService>();
builder.Services.AddSingleton<ILocalErrorLogService, LocalErrorLogService>();
builder.Services.AddHostedService<BrowserLaunchHostedService>();
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Clothes System API", Version = "v1", Description = "服装管理系统 API 文档" });
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");
builder.Services.AddMemoryCache();

var dataRoot = builder.Configuration.GetValue<string>("DataRoot");
if (string.IsNullOrWhiteSpace(dataRoot))
{
    dataRoot = builder.Environment.ContentRootPath;
}

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataRoot, "data-protection-keys")));

var app = builder.Build();

var seedDemoUsers = app.Configuration.GetValue("SeedDemoUsers", false);
var seedDemoClothing = app.Configuration.GetValue("SeedDemoClothing", true);
var resetDemoPasswords = app.Configuration.GetValue("ResetDemoPasswords", false);
var seedDefaultAdmin = app.Configuration.GetValue("SeedDefaultAdmin", true);
var resetDefaultAdminPassword = app.Configuration.GetValue("ResetDefaultAdminPassword", true);
await DbInitializer.InitializeAsync(
    app.Services,
    app.Environment.ContentRootPath,
    seedDemoUsers,
    seedDemoClothing,
    resetDemoPasswords,
    seedDefaultAdmin,
    resetDefaultAdminPassword);

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception)
    {
        var errorLogService = context.RequestServices.GetRequiredService<ILocalErrorLogService>();
        await errorLogService.WriteAsync(exception, context);
        throw;
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clothes System API v1"));
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHealthChecks("/health");

app.Run();
