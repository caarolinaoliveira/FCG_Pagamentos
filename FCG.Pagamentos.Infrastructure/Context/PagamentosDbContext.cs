using FCG.Pagamentos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG.Pagamentos.Infrastructure.Context
{
    public class PagamentosDbContext : DbContext
    {
        public PagamentosDbContext(DbContextOptions<PagamentosDbContext> options)
            : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            ChangeTracker.AutoDetectChangesEnabled = false;
        }

        public DbSet<PagamentoEntity> Pagamentos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(PagamentosDbContext).Assembly
            );
        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries()
                .Where(e => e.Entity.GetType()
                    .GetProperty("ProcessedAt") != null))
            {
                if (entry.State == EntityState.Added)
                    entry.Property("ProcessedAt").CurrentValue = DateTime.UtcNow;
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}