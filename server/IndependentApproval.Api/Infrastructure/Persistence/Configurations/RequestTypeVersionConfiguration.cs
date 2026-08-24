using System.Linq.Expressions;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class RequestTypeVersionConfiguration : IEntityTypeConfiguration<RequestTypeVersion>
{
    private const int AccountMaxLength = 256;

    public void Configure(EntityTypeBuilder<RequestTypeVersion> builder)
    {
        builder.ToTable("RequestTypeVersions", "app", table =>
        {
            table.HasCheckConstraint(
                "CK_RequestTypeVersions_VersionNumber_Positive",
                "[VersionNumber] > 0");
            table.HasCheckConstraint(
                "CK_RequestTypeVersions_NavigationOrder_NonNegative",
                "[NavigationOrder] >= 0");
            table.HasCheckConstraint(
                "CK_RequestTypeVersions_Lifecycle_Valid",
                "[Lifecycle] IN ('Draft', 'Published', 'Archived')");
            table.HasCheckConstraint(
                "CK_RequestTypeVersions_PublishedAudit",
                "[Lifecycle] <> 'Published' OR [PublishedAtUtc] IS NOT NULL");
            table.HasCheckConstraint(
                "CK_RequestTypeVersions_ArchivedAudit",
                "[Lifecycle] <> 'Archived' OR [ArchivedAtUtc] IS NOT NULL");
        });

        builder.HasKey(version => version.Id);

        builder.Property(version => version.Id)
            .ValueGeneratedNever();

        builder.Property(version => version.VersionNumber)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(version => version.NameEnglish)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(version => version.NameArabic)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(version => version.DescriptionEnglish)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(version => version.DescriptionArabic)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(version => version.RequestPrefix)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(30);

        builder.Property(version => version.NavigationSlug)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(200);

        builder.Property(version => version.Lifecycle)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(version => version.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(version => version.CreatedByAccount)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(version => version.ModifiedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(version => version.PublishedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(version => version.ArchivedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(version => version.RowVersion)
            .IsRowVersion();

        builder.HasIndex(version => new { version.RequestTypeId, version.VersionNumber })
            .IsUnique();

        builder.HasAlternateKey(version => new { version.Id, version.RequestTypeId })
            .HasName("AK_RequestTypeVersions_Id_RequestTypeId");

        builder.HasIndex(version => version.RequestTypeId)
            .IsUnique()
            .HasDatabaseName("UX_RequestTypeVersions_RequestTypeId_Draft")
            .HasFilter("[Lifecycle] = 'Draft'");

        builder.HasIndex(version => new { version.Lifecycle, version.NavigationOrder });

        builder.HasIndex(version => new { version.Lifecycle, version.NavigationSlug });

        builder.HasOne(version => version.RequestType)
            .WithMany(requestType => requestType.Versions)
            .HasForeignKey(version => version.RequestTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(version => version.PrefixReservation)
            .WithMany(reservation => reservation.Versions)
            .HasForeignKey(version => new
            {
                version.RequestTypeId,
                version.RequestPrefix
            })
            .HasPrincipalKey(reservation => new
            {
                reservation.RequestTypeId,
                reservation.NormalizedPrefix
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(version => version.SlugReservation)
            .WithMany(reservation => reservation.Versions)
            .HasForeignKey(version => new
            {
                version.RequestTypeId,
                version.NavigationSlug
            })
            .HasPrincipalKey(reservation => new
            {
                reservation.RequestTypeId,
                reservation.NormalizedSlug
            })
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAuditRelationship(builder, version => version.CreatedByUserId);
        ConfigureAuditRelationship(builder, version => version.ModifiedByUserId);
        ConfigureAuditRelationship(builder, version => version.PublishedByUserId);
        ConfigureAuditRelationship(builder, version => version.ArchivedByUserId);
    }

    private static void ConfigureAuditRelationship(
        EntityTypeBuilder<RequestTypeVersion> builder,
        Expression<Func<RequestTypeVersion, object?>> foreignKeyExpression)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
