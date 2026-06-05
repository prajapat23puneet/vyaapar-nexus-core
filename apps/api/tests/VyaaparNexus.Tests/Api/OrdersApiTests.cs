using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Tests.Infrastructure;

namespace VyaaparNexus.Tests.Api;

public class OrdersApiTests : IntegrationTestBase
{
    public OrdersApiTests(VyaaparNexusFactory factory) : base(factory)
    {
    }

    private async Task<CreateOrderRequest> BuildValidCreateOrderRequest()
    {
        var customer = await GetDb().Customers.FirstAsync();
        var product = await GetDb().Products.FirstAsync(x => x.IsActive);

        return new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = product.Id,
                    Quantity = 1
                }
            }
        };
    }

    [Fact]
    public async Task PostOrder_ValidRequest_Returns201()
    {
        var request = await BuildValidCreateOrderRequest();
        var response = await Client.PostAsJsonAsync("/api/v1/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.CorrelationId.Should().NotBeEmpty();
        result.Status.Should().Be("Submitted");
    }

    [Fact]
    public async Task GetOrders_ReturnsCreatedOrder()
    {
        var request = await BuildValidCreateOrderRequest();
        var createResponse = await Client.PostAsJsonAsync("/api/v1/orders", request);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        var response = await Client.GetAsync("/api/v1/orders");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<OrderListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Count.Should().BeGreaterThan(0);
        result.Items.Should().Contain(x => x.Id == createdOrder!.Id);
    }

    [Fact]
    public async Task GetOrderById_ReturnsItemsAndCustomer()
    {
        var request = await BuildValidCreateOrderRequest();
        var createResponse = await Client.PostAsJsonAsync("/api/v1/orders", request);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        var response = await Client.GetAsync($"/api/v1/orders/{createdOrder!.Id}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OrderDetailDto>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Customer.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetOrderSaga_ReturnsStateProjection()
    {
        var request = await BuildValidCreateOrderRequest();
        var createResponse = await Client.PostAsJsonAsync("/api/v1/orders", request);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        var response = await Client.GetAsync($"/api/v1/orders/{createdOrder!.Id}/saga");
        response.EnsureSuccessStatusCode();

        var saga = await response.Content.ReadFromJsonAsync<SagaStateDto>();
        saga.Should().NotBeNull();
        saga!.CurrentState.Should().NotBeNullOrEmpty();
        saga.OrderId.Should().Be(createdOrder.Id);
    }

    [Fact]
    public async Task GetOrderTrace_ReturnsAtLeastOneEvent()
    {
        var request = await BuildValidCreateOrderRequest();
        var createResponse = await Client.PostAsJsonAsync("/api/v1/orders", request);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        var response = await Client.GetAsync($"/api/v1/orders/{createdOrder!.Id}/trace");
        response.EnsureSuccessStatusCode();

        var trace = await response.Content.ReadFromJsonAsync<SagaTraceDto>();
        trace.Should().NotBeNull();
        trace!.Events.Should().HaveCountGreaterThan(0);
        trace.Events.First().EventType.Should().Be("OrderSubmitted");
    }
}

