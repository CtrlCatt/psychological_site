using System.Text.Json;
using System.Text.Json.Serialization;
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
builder.Services.AddSingleton<InMemoryBookings>();
builder.Services.AddSingleton<IBookingRepository>(services => services.GetRequiredService<InMemoryBookings>());
builder.Services.AddSingleton<ICatalogRepository, InMemoryCatalog>();
builder.Services.AddSingleton<IResultRepository, InMemoryResults>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<ResultService>();
builder.Services.AddScoped<BookingService>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapControllers();
app.Run();

public partial class Program;
