using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Domain.Entities;
using VyaaparNexus.Application.Interfaces;

namespace VyaaparNexus.Application.Commands;

public record CreateCustomerCommand(CreateCustomerRequest Request) : IRequest<Guid>;
public record UpdateCustomerCommand(Guid Id, UpdateCustomerRequest Request) : IRequest<Unit>;

public class CustomerCommandsHandler : 
    IRequestHandler<CreateCustomerCommand, Guid>,
    IRequestHandler<UpdateCustomerCommand, Unit>
{
    private readonly IAppDbContext _context;

    public CustomerCommandsHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var c = request.Request;
        var customer = new Customer
        {
            Name = c.Name,
            Email = c.Email,
            Phone = c.Phone,
            AddressLine1 = c.AddressLine1,
            AddressLine2 = c.AddressLine2,
            City = c.City,
            State = c.State,
            Pincode = c.Pincode,
            Country = c.Country
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return customer.Id;
    }

    public async Task<Unit> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers.FindAsync(new object[] { request.Id }, cancellationToken);
        if (customer == null) throw new ArgumentException("Customer not found");

        var c = request.Request;
        customer.Name = c.Name;
        customer.Email = c.Email;
        customer.Phone = c.Phone;
        customer.AddressLine1 = c.AddressLine1;
        customer.AddressLine2 = c.AddressLine2;
        customer.City = c.City;
        customer.State = c.State;
        customer.Pincode = c.Pincode;
        customer.Country = c.Country;
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
