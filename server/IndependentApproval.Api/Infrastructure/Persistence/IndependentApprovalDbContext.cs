using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Documents;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IndependentApprovalDbContext).Assembly);
    }
}
