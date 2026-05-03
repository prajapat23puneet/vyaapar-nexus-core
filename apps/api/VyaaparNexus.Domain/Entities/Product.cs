using System;
using System.Collections.Generic;

namespace VyaaparNexus.Domain.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; } = 0;
    public int ReorderLevel { get; set; } = 10;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int WeightGrams { get; set; } = 0;
    public List<string>? Tags { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    // Navigation property
    public Category? Category { get; set; }
}
