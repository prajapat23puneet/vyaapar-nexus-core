using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Tests.Infrastructure;

namespace VyaaparNexus.Tests.Api;

public class ProductsApiTests : IntegrationTestBase
{
    public ProductsApiTests(VyaaparNexusFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ListProducts_ReturnsActiveProducts()
    {
        var response = await Client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Count.Should().BeGreaterThanOrEqualTo(10);

        foreach (var item in result.Items)
        {
            item.Id.Should().NotBeEmpty();
            item.Sku.Should().NotBeNullOrEmpty();
            item.Name.Should().NotBeNullOrEmpty();
            item.StockQuantity.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task StockPatch_UpdatesCorrectly()
    {
        // Create a dedicated product for this test — fully isolated from seeded products
        var category = await GetDb().Categories.FirstAsync();
        var createResp = await Client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            CategoryId = category.Id,
            Sku = $"STOCK-PATCH-{Guid.NewGuid():N}",
            Name = "Stock Patch Test Product",
            Description = "Used by StockPatch_UpdatesCorrectly test",
            UnitPrice = 10,
            StockQuantity = 50
        });
        createResp.EnsureSuccessStatusCode();
        var newProduct = await createResp.Content.ReadFromJsonAsync<ProductDto>();
        var id = newProduct!.Id;

        var getResponse = await Client.GetAsync($"/api/v1/products/{id}/stock");
        var stockDto = await getResponse.Content.ReadFromJsonAsync<ProductStockDto>();
        stockDto!.StockQuantity.Should().Be(50);

        var patchResponse = await Client.PatchAsJsonAsync($"/api/v1/products/{id}/stock", new AdjustStockRequest { Delta = -1 });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse2 = await Client.GetAsync($"/api/v1/products/{id}/stock");
        var stockDto2 = await getResponse2.Content.ReadFromJsonAsync<ProductStockDto>();
        stockDto2!.StockQuantity.Should().Be(49);
    }

    [Fact]
    public async Task SoftDelete_HidesProductFromActiveList()
    {
        var category = await GetDb().Categories.FirstAsync();

        var createResponse = await Client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            CategoryId = category.Id,
            Sku = $"TEST-DEL-{Guid.NewGuid():N}",
            Name = "To Be Deleted",
            Description = "Test",
            UnitPrice = 10,
            StockQuantity = 100
        });
        var newProduct = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        var newProductId = newProduct!.Id;

        await Client.DeleteAsync($"/api/v1/products/{newProductId}");

        var response = await Client.GetAsync("/api/v1/products");
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<ProductDto>>();

        result!.Items.Should().NotContain(x => x.Id == newProductId);
    }
}

