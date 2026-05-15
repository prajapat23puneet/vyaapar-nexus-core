using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Tests.Infrastructure;

namespace VyaaparNexus.Tests.Api;

public class AnalyticsTests : IntegrationTestBase
{
    public AnalyticsTests(VyaaparNexusFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Summary_ReturnsAllRequiredFields()
    {
        var response = await Client.GetAsync("/api/v1/analytics/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        summary.TryGetProperty("totalOrders", out var totalOrders).Should().BeTrue();
        totalOrders.GetInt32().Should().BeGreaterThanOrEqualTo(0);

        summary.TryGetProperty("completedOrders", out var completedOrders).Should().BeTrue();
        completedOrders.GetInt32().Should().BeGreaterThanOrEqualTo(0);

        summary.TryGetProperty("cancelledOrders", out var cancelledOrders).Should().BeTrue();
        cancelledOrders.GetInt32().Should().BeGreaterThanOrEqualTo(0);

        summary.TryGetProperty("activeSagas", out var activeSagas).Should().BeTrue();
        activeSagas.GetInt32().Should().BeGreaterThanOrEqualTo(0);

        summary.TryGetProperty("outboxPending", out var outboxPending).Should().BeTrue();
        outboxPending.GetInt32().Should().BeGreaterThanOrEqualTo(0);

        summary.TryGetProperty("deadLetterCount", out var deadLetterCount).Should().BeTrue();
        deadLetterCount.GetInt32().Should().BeGreaterThanOrEqualTo(0);

        summary.TryGetProperty("ordersPerMinute", out var ordersPerMinute).Should().BeTrue();
        ordersPerMinute.GetDecimal().Should().BeGreaterThanOrEqualTo(0);

        summary.TryGetProperty("sagaSuccessRate", out var sagaSuccessRate).Should().BeTrue();
        var rate = sagaSuccessRate.GetDecimal();
        rate.Should().BeInRange(0, 1);

        summary.TryGetProperty("p95LatencyMs", out var p95LatencyMs).Should().BeTrue();
        p95LatencyMs.GetInt32().Should().BeGreaterThanOrEqualTo(0);

        summary.TryGetProperty("totalRevenue", out var totalRevenue).Should().BeTrue();
        totalRevenue.GetDecimal().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task PlacingOrder_IncrementsTotal()
    {
        var response1 = await Client.GetAsync("/api/v1/analytics/summary");
        var summary1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        var n = summary1.GetProperty("totalOrders").GetInt32();

        var orderResponse = await Client.PostAsJsonAsync("/api/v1/orders/demo", new { });
        orderResponse.EnsureSuccessStatusCode();

        await Task.Delay(1000); // Wait 1s

        var response2 = await Client.GetAsync("/api/v1/analytics/summary");
        var summary2 = await response2.Content.ReadFromJsonAsync<JsonElement>();
        var newTotal = summary2.GetProperty("totalOrders").GetInt32();

        newTotal.Should().Be(n + 1);
    }

    [Fact]
    public async Task OrdersOverTime_ReturnsArray()
    {
        var response = await Client.GetAsync("/api/v1/analytics/orders-over-time?days=30");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SagaSuccessRate_ReturnsArray()
    {
        var response = await Client.GetAsync("/api/v1/analytics/saga-success-rate?days=7");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task TopProducts_ReturnsArray()
    {
        var response = await Client.GetAsync("/api/v1/analytics/top-products?limit=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
