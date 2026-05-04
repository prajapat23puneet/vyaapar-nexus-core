using System;
using System.Collections.Generic;

namespace VyaaparNexus.Application.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int WeightGrams { get; set; }
    public List<string>? Tags { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class ProductStockDto
{
    public Guid ProductId { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsLowStock { get; set; }
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateProductRequest
{
    public Guid CategoryId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public string? ImageUrl { get; set; }
    public int WeightGrams { get; set; }
    public List<string>? Tags { get; set; }
}

public class UpdateProductRequest : CreateProductRequest { }

public class AdjustStockRequest
{
    public int Delta { get; set; }
}

public class AdjustStockResponse
{
    public Guid ProductId { get; set; }
    public int PreviousStockQuantity { get; set; }
    public int NewStockQuantity { get; set; }
}
