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

public record GetCustomersQuery(int Page = 1, int Size = 20) : IRequest<PaginatedList<CustomerDto>>;
public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDetailDto?>;

public class CustomerQueriesHandler : 
    IRequestHandler<GetCustomersQuery, PaginatedList<CustomerDto>>,
    IRequestHandler<GetCustomerByIdQuery, CustomerDetailDto?>
{
    private readonly IAppDbContext _context;

    public CustomerQueriesHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Customers.AsQueryable();

        var total = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(total / (double)request.Size);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                City = c.City,
                State = c.State,
                Pincode = c.Pincode,
                Country = c.Country,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<CustomerDto>
        {
            Items = items,
            Page = request.Page,
            Size = request.Size,
            Total = total,
            TotalPages = totalPages
        };
    }

    public async Task<CustomerDetailDto?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .Where(c => c.Id == request.Id)
            .Select(c => new CustomerDetailDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                AddressLine1 = c.AddressLine1,
                AddressLine2 = c.AddressLine2,
                City = c.City,
                State = c.State,
                Pincode = c.Pincode,
                Country = c.Country,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (customer == null)
            return null;

        customer.RecentOrders = await _context.Orders
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new RecentOrderDto
            {
                Id = o.Id,
                Status = o.Status.ToString(),
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return customer;
    }
}
