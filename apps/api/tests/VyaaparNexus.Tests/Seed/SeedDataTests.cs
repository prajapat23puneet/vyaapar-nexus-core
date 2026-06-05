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
        var count = await GetDb().Categories.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(7);
    }

    [Fact]
    public async Task ProductsAreSeeded()
    {
        var count = await GetDb().Products.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public async Task CustomersAreSeeded()
    {
        var count = await GetDb().Customers.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task ApiKeyIsSeeded()
    {
        var hasActiveKey = await GetDb().ApiKeys.AnyAsync(x => x.IsActive);
        hasActiveKey.Should().BeTrue();

        var key = await GetDb().ApiKeys.FirstOrDefaultAsync(x => x.IsActive);
        key.Should().NotBeNull();
        key!.KeyHash.Should().NotBeNullOrEmpty();
    }
}

