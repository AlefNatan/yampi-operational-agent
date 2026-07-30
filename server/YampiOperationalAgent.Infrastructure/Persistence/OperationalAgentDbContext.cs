using Microsoft.EntityFrameworkCore;
using YampiOperationalAgent.Domain.Entities;

namespace YampiOperationalAgent.Infrastructure.Persistence;

public sealed class OperationalAgentDbContext(DbContextOptions<OperationalAgentDbContext> options) : DbContext(options)
{
    public const string ConnectionStringName = "OperationalAgent";

    public DbSet<PendingAction> PendingActions => Set<PendingAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperationalAgentDbContext).Assembly);
    }
}
