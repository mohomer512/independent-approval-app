using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Documents;
using IndependentApproval.Api.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace IndependentApproval.Api.Infrastructure.Persistence;

public sealed class IndependentApprovalDbContext(
    DbContextOptions<IndependentApprovalDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public DbSet<ApplicationRole> ApplicationRoles => Set<ApplicationRole>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<ApplicationUserRole> ApplicationUserRoles => Set<ApplicationUserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();

    public DbSet<RequestType> RequestTypes => Set<RequestType>();

    public DbSet<RequestTypeVersion> RequestTypeVersions => Set<RequestTypeVersion>();

    public DbSet<RequestTypePrefixReservation> RequestTypePrefixReservations =>
        Set<RequestTypePrefixReservation>();

    public DbSet<RequestTypeSlugReservation> RequestTypeSlugReservations =>
        Set<RequestTypeSlugReservation>();

    public DbSet<RequestFieldDefinition> RequestFieldDefinitions => Set<RequestFieldDefinition>();

    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    public DbSet<RequestContentValue> RequestContentValues => Set<RequestContentValue>();

    public DbSet<RequestDocument> RequestDocuments => Set<RequestDocument>();

    public DbSet<RequestRichDocumentRevision> RequestRichDocumentRevisions =>
        Set<RequestRichDocumentRevision>();

    public DbSet<RequestNumberSequence> RequestNumberSequences => Set<RequestNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IndependentApprovalDbContext).Assembly);
    }
}
