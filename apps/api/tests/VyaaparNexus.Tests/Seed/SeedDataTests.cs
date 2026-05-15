using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Tests.Infrastructure;

namespace VyaaparNexus.Tests.Seed;

public class SeedDataTests : IntegrationTestBase
{
    public SeedDataTests(VyaaparNexusFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CategoriesAreSeeded()
    {
        var count = await Db.Categories.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(7);
    }

    [Fact]
    public async Task ProductsAreSeeded()
    {
        var count = await Db.Products.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public async Task CustomersAreSeeded()
    {
        var count = await Db.Customers.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task ApiKeyIsSeeded()
    {
        var hasActiveKey = await Db.ApiKeys.AnyAsync(x => x.IsActive);
        hasActiveKey.Should().BeTrue();

        var key = await Db.ApiKeys.FirstOrDefaultAsync(x => x.IsActive);
        key.Should().NotBeNull();
        key!.KeyHash.Should().NotBeNullOrEmpty();
    }
}
