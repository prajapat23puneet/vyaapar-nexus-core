using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Tests.Infrastructure;
using VyaaparNexus.Domain.Enums;

namespace VyaaparNexus.Tests.Saga;

public class PaymentFailureSagaTests : IntegrationTestBase
{
    public PaymentFailureSagaTests(VyaaparNexusFactory factory) : base(factory)
    {
    }

    private async Task<ProductDto> CreateDedicatedProduct(string sku)
    {
        var category = await GetDb().Categories.FirstAsync();
        var createResp = await Client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            CategoryId = category.Id,
            Sku = sku,
            Name = $"Test Product {sku}",
            Description = "Integration test product",
            UnitPrice = 10,
            StockQuantity = 50
        });
        createResp.EnsureSuccessStatusCode();
        return (await createResp.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    [Fact]
    public async Task PaymentFailure_SagaReachesOrderCancelled_StockRestored()
    {
        var product = await CreateDedicatedProduct($"PF-SAGA-A-{Guid.NewGuid():N}");
        var customer = await GetDb().Customers.FirstAsync();

        var request = new CreateOrderRequest
        {
            CustomerId = customer.Id,
            PaymentMethod = PaymentMethod.UPI,
            ShippingAddress = new ShippingAddressDto { Line1 = "Test", City = "Test", State = "TS", Pincode = "123456" },
            Items = new List<CreateOrderItemRequest> { new CreateOrderItemRequest { ProductId = product.Id, Quantity = 1 } }
        };

        var reqMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(request)
        };
        reqMessage.Headers.Add("X-Force-Failure", "payment");

        var response = await Client.SendAsync(reqMessage);
        response.EnsureSuccessStatusCode();

        var order = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        var orderId = order!.Id;

        var saga = await PollSagaUntilTerminal(orderId, 30);

        saga.CurrentState.Should().Be("OrderCancelled");

        var orderResponse = await Client.GetAsync($"/api/v1/orders/{orderId}");
        var orderDetails = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>();
        orderDetails!.FailureReason.Should().NotBeNullOrEmpty();

        // Stock must be fully restored after compensation — give consumer a moment to complete
        await Task.Delay(2000);
        GetDb().ChangeTracker.Clear();
        var finalProduct = await GetDb().Products.FirstAsync(x => x.Id == product.Id);
        finalProduct.StockQuantity.Should().Be(50); // started at 50, reserved then restored
    }

    [Fact]
    public async Task PaymentFailure_TraceContainsCompensationSteps()
    {
        var product = await CreateDedicatedProduct($"PF-SAGA-B-{Guid.NewGuid():N}");
        var customer = await GetDb().Customers.FirstAsync();

        var request = new CreateOrderRequest
        {
            CustomerId = customer.Id,
            PaymentMethod = PaymentMethod.UPI,
            ShippingAddress = new ShippingAddressDto { Line1 = "Test", City = "Test", State = "TS", Pincode = "123456" },
            Items = new List<CreateOrderItemRequest> { new CreateOrderItemRequest { ProductId = product.Id, Quantity = 1 } }
        };

        var reqMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(request)
        };
        reqMessage.Headers.Add("X-Force-Failure", "payment");

        var response = await Client.SendAsync(reqMessage);
        var order = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        var orderId = order!.Id;

        await PollSagaUntilTerminal(orderId, 30);

        var traceResponse = await Client.GetAsync($"/api/v1/orders/{orderId}/trace");
        var trace = await traceResponse.Content.ReadFromJsonAsync<SagaTraceDto>();

        // Trace query returns events oldest-first (OrderBy CreatedAt)
        var eventTypes = trace!.Events.Select(x => x.EventType).ToList();

        // ContainInOrder checks relative order (subsequence) — allows intermediate events like PaymentProcessing
        eventTypes.Should().ContainInOrder(
            "InventoryReserved",
            "PaymentFailed",
            "InventoryReleased",
            "OrderCancelled"
        );
    }
}

