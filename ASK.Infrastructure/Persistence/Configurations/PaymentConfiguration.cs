using ASK.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ASK.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PaymentNumber).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Amount).HasPrecision(18, 4);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Reference).HasMaxLength(200);

        builder.HasIndex(p => p.PaymentNumber).IsUnique();
        builder.HasIndex(p => p.PaidAt);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
