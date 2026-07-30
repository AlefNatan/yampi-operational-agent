using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YampiOperationalAgent.Domain.Entities;
using YampiOperationalAgent.Domain.Enums;

namespace YampiOperationalAgent.Infrastructure.Persistence.Configurations;

internal sealed class PendingActionConfiguration : IEntityTypeConfiguration<PendingAction>
{
    public void Configure(EntityTypeBuilder<PendingAction> builder)
    {
        builder.ToTable("pending_actions");

        builder.HasKey(pendingAction => pendingAction.Id);

        builder.Property(pendingAction => pendingAction.Id)
            .HasColumnName("id");

        builder.Property(pendingAction => pendingAction.ConversationId)
            .HasColumnName("conversation_id")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(pendingAction => pendingAction.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(pendingAction => pendingAction.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(pendingAction => pendingAction.SkuId)
            .HasColumnName("sku_id")
            .IsRequired();

        builder.Property(pendingAction => pendingAction.SkuCode)
            .HasColumnName("sku_code")
            .HasMaxLength(100);

        builder.Property(pendingAction => pendingAction.SkuName)
            .HasColumnName("sku_name")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(pendingAction => pendingAction.CurrentValue)
            .HasColumnName("current_value")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(pendingAction => pendingAction.NewValue)
            .HasColumnName("new_value")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(pendingAction => pendingAction.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(pendingAction => pendingAction.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Property(pendingAction => pendingAction.ConfirmedAtUtc)
            .HasColumnName("confirmed_at_utc");

        builder.Property(pendingAction => pendingAction.CanceledAtUtc)
            .HasColumnName("canceled_at_utc");

        builder.Property(pendingAction => pendingAction.ReplacedAtUtc)
            .HasColumnName("replaced_at_utc");

        builder.Property(pendingAction => pendingAction.ExecutedAtUtc)
            .HasColumnName("executed_at_utc");

        builder.Property(pendingAction => pendingAction.ReplacedByActionId)
            .HasColumnName("replaced_by_action_id");

        builder.Property(pendingAction => pendingAction.FailureMessage)
            .HasColumnName("failure_message")
            .HasMaxLength(1000);

        builder.Property(pendingAction => pendingAction.Version)
            .IsRowVersion();

        builder.HasIndex(pendingAction => new { pendingAction.ConversationId, pendingAction.Status })
            .HasDatabaseName("ix_pending_actions_conversation_id_status");

        builder.HasIndex(pendingAction => pendingAction.ConversationId)
            .HasDatabaseName("ux_pending_actions_active_conversation")
            .IsUnique()
            .HasFilter(
                "\"status\" IN ('"
                + nameof(PendingActionStatus.PendingConfirmation)
                + "', '"
                + nameof(PendingActionStatus.Confirmed)
                + "', '"
                + nameof(PendingActionStatus.Executing)
                + "')");
    }
}
