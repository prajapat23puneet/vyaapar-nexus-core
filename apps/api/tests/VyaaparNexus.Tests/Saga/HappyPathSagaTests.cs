using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Tests.Infrastructure;
using VyaaparNexus.Domain.Enums;

namespace VyaaparNexus.Tests.Saga;

public class HappyPathSagaTests : IntegrationTestBase
{
    public HappyPathSagaTests(VyaaparNexusFactory factory) : base(factory)
    {
    }

    /// <summary>Creates a dedicated product for this test via API so stock is fully controlled and isolated.</summary>
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
    public async Task HappyPath_SagaReachesOrderCompleted()
    {
        var product = await CreateDedicatedProduct($"HP-SAGA-A-{Guid.NewGuid():N}");
        var customer = await GetDb().Customers.FirstAsync();

        var request = new CreateOrderRequest
        {
            CustomerId = customer.Id,
            PaymentMethod = PaymentMethod.UPI,
            ShippingAddress = new ShippingAddressDto { Line1 = "Test", City = "Test", State = "TS", Pincode = "123456" },
            Items = new List<CreateOrderItemRequest> { new CreateOrderItemRequest { ProductId = product.Id, Quantity = 1 } }
        };

        var response = await Client.PostAsJsonAsync("/api/v1/orders", request);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        var orderId = order!.Id;

        var saga = await PollSagaUntilTerminal(orderId, 30);

        saga.CurrentState.Should().Be("OrderCompleted");
        saga.InventoryReserved.Should().BeTrue();
        saga.PaymentProcessed.Should().BeTrue();
        saga.ShippingDispatched.Should().BeTrue();
        saga.NotificationSent.Should().BeTrue();
        saga.DurationMs.Should().NotBeNull().And.BeGreaterThan(0);

        GetDb().ChangeTracker.Clear();
        var finalProduct = await GetDb().Products.FirstAsync(x => x.Id == product.Id);
        finalProduct.StockQuantity.Should().Be(49); // started at 50, ordered 1

        var outboxMessages = await GetDb().OutboxMessages
            .Where(x => x.CorrelationId == saga.CorrelationId)
            .ToListAsync();

        outboxMessages.Should().NotBeEmpty();
        outboxMessages.Should().AllSatisfy(x => x.PublishedAt.Should().NotBeNull());
    }

    [Fact]
    public async Task HappyPath_TraceContainsExpectedSequence()
    {
        var product = await CreateDedicatedProduct($"HP-SAGA-B-{Guid.NewGuid():N}");
        var customer = await GetDb().Customers.FirstAsync();

        var request = new CreateOrderRequest
        {
            CustomerId = customer.Id,
            PaymentMethod = PaymentMethod.UPI,
            ShippingAddress = new ShippingAddressDto { Line1 = "Test", City = "Test", State = "TS", Pincode = "123456" },
            Items = new List<CreateOrderItemRequest> { new CreateOrderItemRequest { ProductId = product.Id, Quantity = 1 } }
        };

        var response = await Client.PostAsJsonAsync("/api/v1/orders", request);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        var orderId = order!.Id;

        await PollSagaUntilTerminal(orderId, 30);

        var traceResponse = await Client.GetAsync($"/api/v1/orders/{orderId}/trace");
        traceResponse.EnsureSuccessStatusCode();

        var trace = await traceResponse.Content.ReadFromJsonAsync<SagaTraceDto>();
        trace.Should().NotBeNull();

        // Trace query returns events oldest-first (OrderBy CreatedAt)
        var eventTypes = trace!.Events.Select(x => x.EventType).ToList();

        // ContainInOrder checks relative order (subsequence) — allows intermediate events between milestones
        eventTypes.Should().ContainInOrder(
            "OrderSubmitted",
            "InventoryReserved",
            "PaymentProcessed",
            "ShippingDispatched",
            "OrderCompleted"
        );
        eventTypes.Should().Contain("NotificationSent");
    }
}

