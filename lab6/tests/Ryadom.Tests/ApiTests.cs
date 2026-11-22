using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ryadom.Tests;

[Trait("Category", "PostgreSql")]
public sealed class ApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>, IDisposable
{
    private readonly WebApplicationFactory<Program> app = postgres.CreateApp();

    [Theory]
    [InlineData("/api/assessments?page=0")]
    [InlineData("/api/specialists?pageSize=51")]
    [InlineData("/api/specialists?unexpected=true")]
    [InlineData("/api/assessments?includeExpress=maybe")]
    [InlineData("/api/assessments?page=1&page=2")]
    [InlineData("/api/specialists?page=")]
    [InlineData("/api/specialists/UPPERCASE/slots")]
    [InlineData("/api/assessments/valid-slug?page=1")]
    [InlineData("/api/results/not-a-uuid")]
    [InlineData("/api/bookings/00000000-0000-0000-0000-000000000000")]
    public async Task InvalidQueriesAndRouteValuesReturn400(string url)
    {
        using var client = await Visitor();
        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CatalogReturnsBoundedPagesAnd404ForUnknownResources()
    {
        using var client = await Visitor();
        var first = await ReadPage(client, "/api/assessments?page=1&pageSize=2");
        var second = await ReadPage(client, "/api/assessments?page=2&pageSize=2");
        var empty = await ReadPage(client, "/api/assessments?page=3&pageSize=2");
        Assert.Equal(4, first.GetProperty("total").GetInt32());
        Assert.Equal(2, first.GetProperty("items").GetArrayLength());
        Assert.True(first.GetProperty("hasNext").GetBoolean());
        Assert.False(second.GetProperty("hasNext").GetBoolean());
        Assert.Equal(0, empty.GetProperty("items").GetArrayLength());
        Assert.NotEqual(first.GetProperty("items")[0].GetProperty("id"), second.GetProperty("items")[0].GetProperty("id"));
        using var missing = await client.GetAsync("/api/assessments/missing");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Theory]
    [InlineData("/api/results", "{")]
    [InlineData("/api/results", "{}")]
    [InlineData("/api/results", "{\"assessmentId\":\"express\",\"answers\":null}")]
    [InlineData("/api/results", "{\"assessmentId\":\"express\",\"answers\":[\"1\",0,0,0]}")]
    [InlineData("/api/results", "{\"assessmentId\":\"express\",\"answers\":[0,0,0,0],\"score\":8}")]
    [InlineData("/api/results", "{\"assessmentId\":\"express\",\"assessmentId\":\"gad-7\",\"answers\":[0]}")]
    [InlineData("/api/bookings", "{\"specialistId\":\"anna\",\"slotId\":\"\"}")]
    [InlineData("/api/bookings", "{\"specialistId\":\"bad specialist\",\"slotId\":\"a\"}")]
    public async Task MalformedOrInvalidBodiesReturn400(string url, string body)
    {
        using var client = await Visitor();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(url, content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpointsRequireBearerAndIgnoreForgedVisitorHeader()
    {
        using var client = app.CreateClient();
        using var absent = await client.GetAsync("/api/results");
        Assert.Equal(HttpStatusCode.Unauthorized, absent.StatusCode);
        using var absentBooking = await client.GetAsync("/api/bookings");
        Assert.Equal(HttpStatusCode.Unauthorized, absentBooking.StatusCode);

        using var registered = await Visitor();
        registered.DefaultRequestHeaders.Add("X-Visitor-Id", Guid.NewGuid().ToString());
        using var authorized = await registered.GetAsync("/api/results");
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
    }

    [Fact]
    public async Task ResultsAreCalculatedOnServerAndIsolatedPerVisitor()
    {
        using var client = await Visitor();
        using var stranger = await Visitor();
        using var created = await client.PostAsJsonAsync("/api/results", new { assessmentId = "express", answers = new[] { 3, 2, 1, 0 } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var url = Assert.IsType<Uri>(created.Headers.Location);

        var result = await client.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal(6, result.GetProperty("score").GetInt32());
        Assert.Equal(12, result.GetProperty("maximum").GetInt32());
        var page = await ReadPage(client, "/api/results?pageSize=1");
        Assert.Equal(1, page.GetProperty("total").GetInt32());
        using var forbiddenToStranger = await stranger.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, forbiddenToStranger.StatusCode);
        Assert.Equal(0, (await ReadPage(stranger, "/api/results")).GetProperty("total").GetInt32());

        using var deleted = await client.DeleteAsync(url);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var missing = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task ResultBodyWithInvalidAnswerCountOrValueReturns422()
    {
        using var client = await Visitor();
        using var wrongCount = await client.PostAsJsonAsync("/api/results", new { assessmentId = "express", answers = new[] { 3 } });
        using var wrongValue = await client.PostAsJsonAsync("/api/results", new { assessmentId = "express", answers = new[] { 4, 0, 0, 0 } });
        using var unknown = await client.PostAsJsonAsync("/api/results", new { assessmentId = "unknown", answers = new[] { 0 } });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongCount.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongValue.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(0, (await ReadPage(client, "/api/results")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task ConcurrentClientsCannotBookTheSameSlot()
    {
        using var first = await Visitor();
        using var second = await Visitor();
        var slot = (await Slots(first, "anna"))[0].GetProperty("id").GetString();
        var body = new { specialistId = "anna", slotId = slot };

        var responses = await Task.WhenAll(
            first.PostAsJsonAsync("/api/bookings", body),
            second.PostAsJsonAsync("/api/bookings", body));

        using var a = responses[0];
        using var b = responses[1];
        var statuses = new[] { a.StatusCode, b.StatusCode };
        Assert.Contains(HttpStatusCode.Created, statuses);
        Assert.Contains(HttpStatusCode.Conflict, statuses);
        Assert.DoesNotContain((await Slots(first, "anna")).EnumerateArray(), item => item.GetProperty("id").GetString() == slot);
    }

    [Fact]
    public async Task RescheduleAndCancelReleaseThePreviousSlots()
    {
        using var client = await Visitor();
        using var stranger = await Visitor();
        var slots = await Slots(client, "anna");
        var first = slots[0].GetProperty("id").GetString();
        var second = slots[1].GetProperty("id").GetString();
        var foreign = (await Slots(client, "elena"))[0].GetProperty("id").GetString();
        using var created = await client.PostAsJsonAsync("/api/bookings", new { specialistId = "anna", slotId = first });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var url = Assert.IsType<Uri>(created.Headers.Location);

        using var wrongOwner = await stranger.DeleteAsync(url);
        using var wrongSpecialist = await client.PutAsJsonAsync(url, new { slotId = foreign });
        using var invalidUpdate = await client.PutAsJsonAsync(url, new { slotId = "" });
        Assert.Equal(HttpStatusCode.NotFound, wrongOwner.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongSpecialist.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidUpdate.StatusCode);
        using var moved = await client.PutAsJsonAsync(url, new { slotId = second });
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        var current = await client.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal(second, current.GetProperty("slotId").GetString());
        Assert.Contains((await Slots(client, "anna")).EnumerateArray(), item => item.GetProperty("id").GetString() == first);

        using var cancelled = await client.DeleteAsync(url);
        Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        Assert.Contains((await Slots(client, "anna")).EnumerateArray(), item => item.GetProperty("id").GetString() == second);
        Assert.Equal(0, (await ReadPage(client, "/api/bookings")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task PostgreSqlDataSurvivesApiRestart()
    {
        using var anotherApp = postgres.CreateApp();
        Uri resultLocation;
        Uri bookingLocation;
        string token;
        using (var writerApp = postgres.CreateApp())
        using (var writer = writerApp.CreateClient())
        {
            token = await Register(writer);
            using var result = await writer.PostAsJsonAsync("/api/results",
                new { assessmentId = "express", answers = new[] { 3, 0, 0, 0 } });
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            resultLocation = Assert.IsType<Uri>(result.Headers.Location);

            var slot = (await Slots(writer, "mikhail"))[0].GetProperty("id").GetString();
            using var booking = await writer.PostAsJsonAsync("/api/bookings",
                new { specialistId = "mikhail", slotId = slot });
            Assert.Equal(HttpStatusCode.Created, booking.StatusCode);
            bookingLocation = Assert.IsType<Uri>(booking.Headers.Location);
        }

        using var reader = anotherApp.CreateClient();
        reader.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resultAfterRestart = await reader.GetFromJsonAsync<JsonElement>(resultLocation);
        var bookingAfterRestart = await reader.GetFromJsonAsync<JsonElement>(bookingLocation);
        Assert.Equal(3, resultAfterRestart.GetProperty("score").GetInt32());
        Assert.Equal("mikhail", bookingAfterRestart.GetProperty("specialistId").GetString());
    }

    [Fact]
    public async Task PostgreSqlReturnsTenSlotsByDefault()
    {
        using var client = await Visitor();
        var first = await ReadPage(client, "/api/specialists/elena/slots?page=1");
        var second = await ReadPage(client, "/api/specialists/elena/slots?page=2");
        Assert.Equal(10, first.GetProperty("pageSize").GetInt32());
        Assert.Equal(10, first.GetProperty("items").GetArrayLength());
        Assert.Equal(10, second.GetProperty("items").GetArrayLength());
        Assert.True(first.GetProperty("hasNext").GetBoolean());
        Assert.NotEqual(first.GetProperty("items")[0].GetProperty("id").GetString(),
            second.GetProperty("items")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task UnsupportedMethodAndMediaTypeReturn405And415()
    {
        using var client = await Visitor();
        using var method = await client.DeleteAsync("/api/assessments/express");
        using var body = new StringContent("express");
        using var media = await client.PostAsync("/api/results", body);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, method.StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, media.StatusCode);
    }

    public void Dispose() => app.Dispose();

    private async Task<HttpClient> Visitor()
    {
        var client = app.CreateClient();
        await Register(client);
        return client;
    }

    private static async Task<string> Register(HttpClient client)
    {
        var username = "u" + Guid.NewGuid().ToString("N")[..24];
        using var response = await client.PostAsJsonAsync("/api/auth/register",
            new { userName = username, password = "TestPassword2026!" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = result.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return token;
    }

    private static Task<JsonElement> ReadPage(HttpClient client, string path) => client.GetFromJsonAsync<JsonElement>(path);

    private static async Task<JsonElement> Slots(HttpClient client, string specialist) =>
        (await ReadPage(client, $"/api/specialists/{specialist}/slots?pageSize=50")).GetProperty("items");
}
