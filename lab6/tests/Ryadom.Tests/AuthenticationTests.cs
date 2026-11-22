using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Ryadom.Api;
using Ryadom.Application;

namespace Ryadom.Tests;

[Trait("Category", "PostgreSql")]
public sealed class AuthenticationTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>, IDisposable
{
    private readonly WebApplicationFactory<Program> app = postgres.CreateApp();

    [Fact]
    public async Task RegisterIssuesTokenAndStoresOnlyHashedPassword()
    {
        using var client = app.CreateClient();
        var username = "u" + Guid.NewGuid().ToString("N")[..25];
        var password = "SuperStrongPassword123!";
        using var response = await client.PostAsJsonAsync("/api/auth/register", new { userName = username, password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("User", data.GetProperty("role").GetString());
        var token = data.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/results")).StatusCode);

        var configuration = app.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        await using var connection = new NpgsqlConnection(configuration.GetConnectionString("Ryadom"));
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT password_hash FROM app_users WHERE username = @username", connection);
        cmd.Parameters.AddWithValue("username", username);
        var hash = (string)(await cmd.ExecuteScalarAsync())!;
        Assert.NotEqual(password, hash);
        Assert.DoesNotContain(password, hash, StringComparison.Ordinal);
        Assert.True(hash.Length >= 40);
        Assert.True(new StrongPasswordService().Verify(new Account(Guid.Empty, username, hash, "User", DateTimeOffset.UtcNow), password));
    }

    [Fact]
    public async Task LoginWrongPasswordDuplicateUsernameAndInvalidInputAreRejected()
    {
        using var client = app.CreateClient();
        var username = "u" + Guid.NewGuid().ToString("N")[..24];
        var body = new { userName = username, password = "Password123456!" };
        using var registered = await client.PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        using var duplicate = await client.PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var invalid = await client.PostAsJsonAsync("/api/auth/register", new { userName = "a", password = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var rejected = await client.PostAsJsonAsync("/api/auth/login", new { userName = username, password = "wrongPassword000" });
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        using var success = await client.PostAsJsonAsync("/api/auth/login", body);
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
    }

    [Fact]
    public async Task UserIsForbiddenFromAdminButAdminIsAllowed()
    {
        using var client = app.CreateClient();
        using var anonymous = await client.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        using var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            userName = "u" + Guid.NewGuid().ToString("N")[..20], password = "Password123456!"
        });
        var normal = await registration.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", normal.GetProperty("accessToken").GetString());
        using var forbidden = await client.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        using var adminLogin = await client.PostAsJsonAsync("/api/auth/login",
            new { userName = postgres.AdminUserName, password = postgres.AdminPassword });
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);
        var admin = await adminLogin.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Admin", admin.GetProperty("role").GetString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.GetProperty("accessToken").GetString());
        using var allowed = await client.GetAsync("/api/admin/users?pageSize=10");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        var json = await allowed.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BadBearerTokenDoesNotGrantAccess()
    {
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "this-is-not-a-jwt");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/bookings")).StatusCode);
    }

    public void Dispose() => app.Dispose();
}
