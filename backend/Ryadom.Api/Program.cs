using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
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

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapControllers();
app.Run();

public partial class Program;
