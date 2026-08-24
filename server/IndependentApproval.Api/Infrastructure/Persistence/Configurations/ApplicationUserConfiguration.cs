using IndependentApproval.Api.Domain.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    private const int AccountMaxLength = 256;

    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUsers", "app");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .ValueGeneratedNever();

        var sidProperty = builder.Property(user => user.AdSid)
            .IsRequired()
            .HasColumnType("varbinary(68)");
        sidProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(user => user.AdObjectGuid);

        builder.Property(user => user.AccountName)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(user => user.Domain)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(user => user.SamAccountName)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(user => user.UserPrincipalName)
            .HasMaxLength(320);

        builder.Property(user => user.NormalizedAccountName)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(user => user.DisplayName)
            .HasMaxLength(256);

        builder.Property(user => user.Email)
            .HasMaxLength(320);

        builder.Property(user => user.IsActive)
            .HasDefaultValue(true);

        builder.Property(user => user.IsLocked)
            .HasDefaultValue(false);

        builder.Property(user => user.LockReason)
            .HasMaxLength(1000);

        builder.Property(user => user.IsRemoved)
            .HasDefaultValue(false);

        builder.Property(user => user.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(user => user.CreatedByAccount)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(user => user.ModifiedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(user => user.LockedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(user => user.RemovedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(user => user.RowVersion)
            .IsRowVersion();

        builder.HasIndex(user => user.AdSid)
            .IsUnique();

        builder.HasIndex(user => user.AdObjectGuid)
            .IsUnique()
            .HasFilter("[AdObjectGuid] IS NOT NULL");

        builder.HasIndex(user => user.NormalizedAccountName)
            .IsUnique();

        builder.HasIndex(user => new { user.IsRemoved, user.IsActive, user.IsLocked });

        ConfigureAuditRelationship(builder, user => user.CreatedByUserId);
        ConfigureAuditRelationship(builder, user => user.ModifiedByUserId);
        ConfigureAuditRelationship(builder, user => user.LockedByUserId);
        ConfigureAuditRelationship(builder, user => user.RemovedByUserId);
    }

    private static void ConfigureAuditRelationship(
        EntityTypeBuilder<ApplicationUser> builder,
        System.Linq.Expressions.Expression<Func<ApplicationUser, object?>> foreignKeyExpression)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
