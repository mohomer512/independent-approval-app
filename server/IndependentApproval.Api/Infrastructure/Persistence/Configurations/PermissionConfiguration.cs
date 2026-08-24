using IndependentApproval.Api.Domain.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", "app");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Id)
            .ValueGeneratedNever();

        var codeProperty = builder.Property(permission => permission.Code)
            .IsRequired()
            .HasMaxLength(100);
        codeProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(permission => permission.NameEnglish)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(permission => permission.NameArabic)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(permission => permission.DescriptionEnglish)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(permission => permission.DescriptionArabic)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(permission => permission.Code)
            .IsUnique();

        builder.HasData(PermissionCatalog.All.Select(definition => new Permission
        {
            Id = definition.Id,
            Code = definition.Code,
            NameEnglish = definition.NameEnglish,
            NameArabic = definition.NameArabic,
            DescriptionEnglish = definition.DescriptionEnglish,
            DescriptionArabic = definition.DescriptionArabic
        }));
    }
}
