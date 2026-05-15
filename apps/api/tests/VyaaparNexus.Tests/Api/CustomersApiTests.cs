using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Tests.Infrastructure;

namespace VyaaparNexus.Tests.Api;

public class CustomersApiTests : IntegrationTestBase
{
    public CustomersApiTests(VyaaparNexusFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ListCustomers_ReturnsFiveSeededCustomers()
    {
        var response = await Client.GetAsync("/api/v1/customers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<CustomerDto>>();
        result.Should().NotBeNull();
        result!.Total.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task CreateCustomer_Returns201()
    {
        var uniqueEmail = $"test-{Guid.NewGuid()}@example.com";
        var request = new CreateCustomerRequest
        {
            Name = "Test Customer",
            Email = uniqueEmail,
            Phone = "+1234567890",
            AddressLine1 = "123 Test St"
        };

        var response = await Client.PostAsJsonAsync("/api/v1/customers", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
        customer!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateCustomer_Returns200()
    {
        var customer = await Db.Customers.FirstAsync();
        var id = customer.Id;
        var newName = "Updated Name " + Guid.NewGuid();

        var request = new UpdateCustomerRequest
        {
            Name = newName,
            Email = customer.Email,
            Phone = customer.Phone,
            AddressLine1 = "123 Test St"
        };

        var response = await Client.PutAsJsonAsync($"/api/v1/customers/{id}", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        Db.ChangeTracker.Clear();
        var updatedCustomer = await Db.Customers.FirstAsync(x => x.Id == id);
        updatedCustomer.Name.Should().Be(newName);
    }
}
