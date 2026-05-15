using System.Net;
using System.Text.Json;
using FluentAssertions;
using VyaaparNexus.Tests.Infrastructure;

namespace VyaaparNexus.Tests.Api;

public class StreamTests : IntegrationTestBase
{
    public StreamTests(VyaaparNexusFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Stream_RequiresApiKey()
    {
        Client.DefaultRequestHeaders.Remove("X-Api-Key");

        var response = await Client.GetAsync("/api/stream");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Stream_Returns_TextEventStream_ContentType()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/stream");
        
        using var response = await Client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");
    }

    [Fact]
    public async Task Stream_Emits_ParseableEvents_WithRequiredFields()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/stream");
        
        using var response = await Client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        var events = new List<JsonDocument>();
        while (!cts.IsCancellationRequested && events.Count < 3)
        {
            var line = await reader.ReadLineAsync(cts.Token);
            if (line is null) break;
            if (line.StartsWith("data: "))
            {
                var json = line["data: ".Length..];
                events.Add(JsonDocument.Parse(json));
            }
        }

        events.Should().HaveCount(3);

        foreach (var evt in events)
        {
            evt.RootElement.TryGetProperty("activeSagas", out _).Should().BeTrue();
            evt.RootElement.TryGetProperty("outboxPending", out _).Should().BeTrue();
            evt.RootElement.TryGetProperty("timestamp", out _).Should().BeTrue();
            
            evt.RootElement.TryGetProperty("circuitStates", out var circuitStates).Should().BeTrue();
            circuitStates.ValueKind.Should().Be(JsonValueKind.Object);
            circuitStates.TryGetProperty("payment", out _).Should().BeTrue();
        }
    }
}
