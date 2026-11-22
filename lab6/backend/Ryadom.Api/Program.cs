using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using Ryadom.Api;
using Ryadom.Application;
using Ryadom.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers(options => options.Filters.Add<ValidateRequestParameters>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        options.JsonSerializerOptions.AllowDuplicateProperties = false;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
    });
// Секрет не хранится в исходниках. Для разработки задаётся через Jwt__Key.
var jwtSecret = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    throw new InvalidOperationException("Задайте Jwt__Key: случайный секрет длиной не менее 32 байт.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = "Ryadom",
            ValidateAudience = true,
            ValidAudience = "RyadomAngular",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
            NameClaimType = "unique_name"
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiErrors>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(services =>
{
    var config = services.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Ryadom");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Укажите строку подключения ConnectionStrings__Ryadom.");
    return NpgsqlDataSource.Create(connectionString);
});
builder.Services.AddDbContext<RyadomDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<NpgsqlDataSource>()));
builder.Services.AddScoped<ICatalogRepository, EfCatalogRepository>();
builder.Services.AddScoped<IResultRepository, EfResultRepository>();
builder.Services.AddScoped<IBookingRepository, SqlBookingRepository>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<ResultService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<IAccountRepository, EfAccountRepository>();
builder.Services.AddSingleton<IPasswordService, StrongPasswordService>();
builder.Services.AddScoped<IAccessTokenIssuer, JwtTokenIssuer>();
builder.Services.AddScoped<AuthenticationService>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

// Инициализация демонстрационного администратора разрешена ТОЛЬКО в Development.
// Логин и пароль вводятся через переменные окружения, в БД сохраняется лишь хеш.
if (app.Environment.IsDevelopment())
{
    var adminName = app.Configuration["AdminBootstrap:Username"];
    var adminPassword = app.Configuration["AdminBootstrap:Password"];
    if (!string.IsNullOrWhiteSpace(adminName) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AuthenticationService>()
            .EnsureDevelopmentAdmin(adminName, adminPassword);
    }
}
app.MapControllers();
app.Run();

public partial class Program;
