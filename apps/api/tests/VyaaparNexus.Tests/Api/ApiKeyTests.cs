using System.Net;
using FluentAssertions;
using VyaaparNexus.Tests.Infrastructure;

namespace VyaaparNexus.Tests.Api;

public class ApiKeyTests : IntegrationTestBase
{
    public ApiKeyTests(VyaaparNexusFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task MissingApiKey_Returns401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Remove("X-Api-Key");

        // Act
        var response = await Client.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidApiKey_Returns401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Remove("X-Api-Key");
        Client.DefaultRequestHeaders.Add("X-Api-Key", "invalid-key-9999");

        // Act
        var response = await Client.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidApiKey_Returns200OnProtectedEndpoint()
    {
        // Arrange - Base class already adds valid key

        // Act
        var response = await Client.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
