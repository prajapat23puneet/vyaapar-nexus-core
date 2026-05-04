using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Domain.Entities;
using VyaaparNexus.Application.Interfaces;

namespace VyaaparNexus.Application.Commands;

public record CreateProductCommand(CreateProductRequest Request) : IRequest<Guid>;
public record UpdateProductCommand(Guid Id, UpdateProductRequest Request) : IRequest<Unit>;
public record AdjustStockCommand(Guid Id, AdjustStockRequest Request) : IRequest<AdjustStockResponse>;
public record DeleteProductCommand(Guid Id) : IRequest<Unit>;

public class ProductCommandsHandler : 
    IRequestHandler<CreateProductCommand, Guid>,
    IRequestHandler<UpdateProductCommand, Unit>,
    IRequestHandler<AdjustStockCommand, AdjustStockResponse>,
    IRequestHandler<DeleteProductCommand, Unit>
{
    private readonly IAppDbContext _context;

    public ProductCommandsHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var p = request.Request;
        var product = new Product
        {
            CategoryId = p.CategoryId,
            Sku = p.Sku,
            Name = p.Name,
            Description = p.Description,
            Brand = p.Brand,
            UnitPrice = p.UnitPrice,
            StockQuantity = p.StockQuantity,
            ReorderLevel = p.ReorderLevel,
            ImageUrl = p.ImageUrl,
            WeightGrams = p.WeightGrams,
            Tags = p.Tags,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }

    public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync(new object[] { request.Id }, cancellationToken);
        if (product == null) throw new ArgumentException("Product not found");

        var p = request.Request;
        product.CategoryId = p.CategoryId;
        product.Sku = p.Sku;
        product.Name = p.Name;
        product.Description = p.Description;
        product.Brand = p.Brand;
        product.UnitPrice = p.UnitPrice;
        product.StockQuantity = p.StockQuantity;
        product.ReorderLevel = p.ReorderLevel;
        product.ImageUrl = p.ImageUrl;
        product.WeightGrams = p.WeightGrams;
        product.Tags = p.Tags;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<AdjustStockResponse> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync(new object[] { request.Id }, cancellationToken);
        if (product == null) throw new ArgumentException("Product not found");

        int previousStock = product.StockQuantity;
        int newStock = previousStock + request.Request.Delta;

        if (newStock < 0) throw new ArgumentException("Stock cannot be negative.");

        product.StockQuantity = newStock;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new AdjustStockResponse
        {
            ProductId = product.Id,
            PreviousStockQuantity = previousStock,
            NewStockQuantity = newStock
        };
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync(new object[] { request.Id }, cancellationToken);
        if (product != null)
        {
            product.IsActive = false;
            product.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
