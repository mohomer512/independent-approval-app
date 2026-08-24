using System.Linq.Expressions;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class RequestFieldDefinitionConfiguration : IEntityTypeConfiguration<RequestFieldDefinition>
{
    private const int AccountMaxLength = 256;

    public void Configure(EntityTypeBuilder<RequestFieldDefinition> builder)
    {
        builder.ToTable("RequestFieldDefinitions", "app", table =>
        {
            table.HasCheckConstraint(
                "CK_RequestFieldDefinitions_SortOrder_NonNegative",
                "[SortOrder] >= 0");
            table.HasCheckConstraint(
                "CK_RequestFieldDefinitions_FieldType_Valid",
                "[FieldType] IN ('ShortText', 'LongText', 'Integer', 'Decimal', 'Date', " +
                "'DateTime', 'YesNo', 'SingleChoice', 'MultipleChoice', 'ActiveDirectoryUser', " +
                "'ApplicationRole', 'FileDocument', 'RichDocument')");
            table.HasCheckConstraint(
                "CK_RequestFieldDefinitions_DocumentMode_Valid",
                "[DocumentMode] IS NULL OR [DocumentMode] IN " +
                "('UploadOnly', 'CreateInEditorOnly', 'UploadOrCreate')");
            table.HasCheckConstraint(
                "CK_RequestFieldDefinitions_DocumentMode_FieldType",
                "(([FieldType] IN ('FileDocument', 'RichDocument') AND [DocumentMode] IS NOT NULL) " +
                "OR ([FieldType] NOT IN ('FileDocument', 'RichDocument') AND [DocumentMode] IS NULL))");
            table.HasCheckConstraint(
                "CK_RequestFieldDefinitions_DefaultValueJson_Valid",
                "[DefaultValueJson] IS NULL OR ISJSON(CONCAT('[', [DefaultValueJson], ']')) = 1");
            table.HasCheckConstraint(
                "CK_RequestFieldDefinitions_ValidationConfigurationJson_Valid",
                "[ValidationConfigurationJson] IS NULL OR ISJSON([ValidationConfigurationJson]) = 1");
            table.HasCheckConstraint(
                "CK_RequestFieldDefinitions_ChoiceConfigurationJson_Valid",
                "[ChoiceConfigurationJson] IS NULL OR ISJSON([ChoiceConfigurationJson]) = 1");
        });

        builder.HasKey(field => field.Id);

        builder.Property(field => field.Id)
            .ValueGeneratedNever();

        var keyProperty = builder.Property(field => field.Key)
            .IsRequired()
            .HasMaxLength(100);
        keyProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        var normalizedKeyProperty = builder.Property(field => field.NormalizedKey)
            .IsRequired()
            .HasMaxLength(100);
        normalizedKeyProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(field => field.LabelEnglish)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(field => field.LabelArabic)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(field => field.HelpTextEnglish)
            .HasMaxLength(1000);

        builder.Property(field => field.HelpTextArabic)
            .HasMaxLength(1000);

        builder.Property(field => field.FieldType)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(field => field.DefaultValueJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(field => field.ValidationConfigurationJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(field => field.ChoiceConfigurationJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(field => field.IsActive)
            .HasDefaultValue(true);

        builder.Property(field => field.DocumentMode)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(field => field.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(field => field.CreatedByAccount)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(field => field.ModifiedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(field => field.RowVersion)
            .IsRowVersion();

        builder.HasIndex(field => new { field.RequestTypeVersionId, field.NormalizedKey })
            .IsUnique();

        builder.HasAlternateKey(field => new
        {
            field.Id,
            field.RequestTypeVersionId
        })
            .HasName("AK_RequestFieldDefinitions_Id_RequestTypeVersionId");

        builder.HasIndex(field => new { field.RequestTypeVersionId, field.SortOrder })
            .IsUnique();

        builder.HasIndex(field => new { field.RequestTypeVersionId, field.IsActive });

        builder.HasOne(field => field.RequestTypeVersion)
            .WithMany(version => version.Fields)
            .HasForeignKey(field => field.RequestTypeVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAuditRelationship(builder, field => field.CreatedByUserId);
        ConfigureAuditRelationship(builder, field => field.ModifiedByUserId);
    }

    private static void ConfigureAuditRelationship(
        EntityTypeBuilder<RequestFieldDefinition> builder,
        Expression<Func<RequestFieldDefinition, object?>> foreignKeyExpression)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
