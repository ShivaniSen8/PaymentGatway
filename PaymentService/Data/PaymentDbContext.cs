using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Amount)
                  .HasPrecision(18, 2);

            entity.Property(p => p.Currency)
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(p => p.PaymentProvider)
                  .HasMaxLength(50);

            entity.Property(p => p.ProviderOrderId)
                  .HasMaxLength(200);

            entity.Property(p => p.ProviderPaymentId)
                  .HasMaxLength(200);

            entity.Property(p => p.IdempotencyKey)
                  .HasMaxLength(200);

            entity.HasIndex(p => p.OrderId);

            entity.HasIndex(p => p.IdempotencyKey)
                  .IsUnique();
        });
    }
}