using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Domain.Entities;

namespace VyaaparNexus.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<SagaState> SagaStates { get; }
    DbSet<SagaEventLog> SagaEventLogs { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<InboxMessage> InboxMessages { get; }
    DbSet<ApiKey> ApiKeys { get; }
    DbSet<MetricsSnapshot> MetricsSnapshots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
