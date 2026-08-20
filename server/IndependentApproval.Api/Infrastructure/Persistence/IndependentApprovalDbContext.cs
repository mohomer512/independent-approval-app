using IndependentApproval.Api.Domain.Documents;
using IndependentApproval.Api.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace IndependentApproval.Api.Infrastructure.Persistence;

public sealed class IndependentApprovalDbContext(
    DbContextOptions<IndependentApprovalDbContext> options) : DbContext(options)
{
    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new DocumentRecordConfiguration());
    }
}
