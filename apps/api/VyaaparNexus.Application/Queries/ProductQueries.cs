using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Application.Interfaces;

namespace VyaaparNexus.Application.Queries;

public record GetProductsQuery(int Page = 1, int Size = 20, Guid? CategoryId = null, string? Search = null, bool IncludeInactive = false) : IRequest<PaginatedList<ProductDto>>;
public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
public record GetProductStockQuery(Guid Id) : IRequest<ProductStockDto?>;

public class ProductQueriesHandler : 
    IRequestHandler<GetProductsQuery, PaginatedList<ProductDto>>,
    IRequestHandler<GetProductByIdQuery, ProductDto?>,
    IRequestHandler<GetProductStockQuery, ProductStockDto?>
{
    private readonly IAppDbContext _context;

    public ProductQueriesHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products.Include(p => p.Category).AsQueryable();

        if (!request.IncludeInactive)
            query = query.Where(p => p.IsActive);

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) || p.Sku.ToLower().Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(total / (double)request.Size);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                Brand = p.Brand,
                UnitPrice = p.UnitPrice,
                StockQuantity = p.StockQuantity,
                ReorderLevel = p.ReorderLevel,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                WeightGrams = p.WeightGrams,
                Tags = p.Tags != null ? p.Tags.ToList() : new List<string>(),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<ProductDto>
        {
            Items = items,
            Page = request.Page,
            Size = request.Size,
            Total = total,
            TotalPages = totalPages
        };
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                Brand = p.Brand,
                UnitPrice = p.UnitPrice,
                StockQuantity = p.StockQuantity,
                ReorderLevel = p.ReorderLevel,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                WeightGrams = p.WeightGrams,
                Tags = p.Tags != null ? p.Tags.ToList() : new List<string>(),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return product;
    }

    public async Task<ProductStockDto?> Handle(GetProductStockQuery request, CancellationToken cancellationToken)
    {
        return await _context.Products
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductStockDto
            {
                ProductId = p.Id,
                StockQuantity = p.StockQuantity,
                ReorderLevel = p.ReorderLevel,
                IsLowStock = p.StockQuantity <= p.ReorderLevel
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
