using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace Ryadom.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly string schema = "lab5_test_" + Guid.NewGuid().ToString("N");
    private NpgsqlDataSource? admin;
    private string connectionString = "";
    public string AdminUserName { get; } = "admin" + Guid.NewGuid().ToString("N")[..10];
    public string AdminPassword { get; } = "Example!" + Guid.NewGuid().ToString("N");

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("RYADOM_TEST_DB");
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Укажите RYADOM_TEST_DB для интеграционных тестов PostgreSQL. См. README.");

        admin = NpgsqlDataSource.Create(configured);
        await using (var create = admin.CreateCommand($"CREATE SCHEMA {schema}"))
            await create.ExecuteNonQueryAsync();

        var settings = new NpgsqlConnectionStringBuilder(configured) { SearchPath = schema, Pooling = false };
        connectionString = settings.ConnectionString;
        await using var testDatabase = NpgsqlDataSource.Create(connectionString);
        foreach (var file in new[] { "01-schema.sql", "02-seed.sql", "03-authentication.sql" })
        {
            var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "database", file));
            await using var command = testDatabase.CreateCommand(sql);
            await command.ExecuteNonQueryAsync();
        }
    }

    public WebApplicationFactory<Program> CreateApp() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development")
                .UseSetting("ConnectionStrings:Ryadom", connectionString)
                .UseSetting("Jwt:Key", "integration-test-only-secret-strong-32-bytes-please-change")
                .UseSetting("AdminBootstrap:Username", AdminUserName)
                .UseSetting("AdminBootstrap:Password", AdminPassword));

    public async Task DisposeAsync()
    {
        if (admin is null) return;
        await using var dataSource = admin;
        await using var drop = dataSource.CreateCommand($"DROP SCHEMA IF EXISTS {schema} CASCADE");
        await drop.ExecuteNonQueryAsync();
    }
}
