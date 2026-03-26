using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Configuration
{
    public class NotificationHistoryConfiguration : IEntityTypeConfiguration<NotificationHistory>
    {
        public void Configure(EntityTypeBuilder<NotificationHistory> builder)
        {
            builder.HasOne(n => n.ActorProfile)
                .WithMany()
                .HasForeignKey(n => n.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(n => n.ReceiverProfile)
                .WithMany()
                .HasForeignKey(n => n.ReceiverUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
} 