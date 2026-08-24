using IndependentApproval.Api.Domain.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    private const int AccountMaxLength = 256;

    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("ApplicationRoles", "app", table =>
            table.HasCheckConstraint(
                "CK_ApplicationRoles_ArchivedInactive",
                "[IsArchived] = 0 OR [IsActive] = 0"));

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Id)
            .ValueGeneratedNever();

        var codeProperty = builder.Property(role => role.Code)
            .IsRequired()
            .HasMaxLength(100);
        codeProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        var normalizedCodeProperty = builder.Property(role => role.NormalizedCode)
            .IsRequired()
            .HasMaxLength(100);
        normalizedCodeProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(role => role.NameEnglish)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(role => role.NameArabic)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(role => role.DescriptionEnglish)
            .HasMaxLength(2000);

        builder.Property(role => role.DescriptionArabic)
            .HasMaxLength(2000);

        builder.Property(role => role.IsActive)
            .HasDefaultValue(true);

        builder.Property(role => role.IsArchived)
            .HasDefaultValue(false);

        builder.Property(role => role.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(role => role.CreatedByAccount)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(role => role.ModifiedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(role => role.ArchivedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(role => role.RowVersion)
            .IsRowVersion();

        builder.HasIndex(role => role.NormalizedCode)
            .IsUnique();

        builder.HasIndex(role => new { role.IsArchived, role.IsActive });

        ConfigureAuditRelationship(builder, role => role.CreatedByUserId);
        ConfigureAuditRelationship(builder, role => role.ModifiedByUserId);
        ConfigureAuditRelationship(builder, role => role.ArchivedByUserId);
    }

    private static void ConfigureAuditRelationship(
        EntityTypeBuilder<ApplicationRole> builder,
        System.Linq.Expressions.Expression<Func<ApplicationRole, object?>> foreignKeyExpression)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
