using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Domain.Entities;
using Newtonsoft.Json;

namespace VyaaparNexus.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, string basePath)
    {
        await context.Database.MigrateAsync();

        if (!await context.ApiKeys.AnyAsync())
        {
            var demoKey = "vyaaparnexus-demo-key-2026";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(demoKey));
            var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

            context.ApiKeys.Add(new ApiKey
            {
                Id = Guid.NewGuid(),
                KeyHash = hashString,
                Name = "Demo Key",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();
        }

        if (!await context.Customers.AnyAsync())
        {
            var path = Path.Combine(basePath, "Persistence", "Seed", "customers.seed.json");
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path);
                var customers = JsonConvert.DeserializeObject<List<Customer>>(json);
                if (customers != null && customers.Any())
                {
                    context.Customers.AddRange(customers);
                    await context.SaveChangesAsync();
                }
            }
        }

        if (!await context.Categories.AnyAsync() && !await context.Products.AnyAsync())
        {
            var path = Path.Combine(basePath, "Persistence", "Seed", "products.seed.json");
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path);
                var root = JsonConvert.DeserializeObject<ProductSeedRoot>(json);
                if (root != null)
                {
                    if (root.Categories != null && root.Categories.Any())
                    {
                        context.Categories.AddRange(root.Categories);
                    }
                    if (root.Products != null && root.Products.Any())
                    {
                        context.Products.AddRange(root.Products);
                    }
                    await context.SaveChangesAsync();
                }
            }
        }
    }

    public class ProductSeedRoot
    {
        public List<Category>? Categories { get; set; }
        public List<Product>? Products { get; set; }
    }
}
