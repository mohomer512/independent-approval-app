using System.Buffers.Binary;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IndependentApproval.Api.Tests.Infrastructure;

public sealed class AdministrationApiFixture : IAsyncLifetime
{
    public const string BootstrapAdministratorAccount = "TEST\\sp.admin";
    public const string ApplicationUserAccount = "TEST\\member";
    public const string LockedUserAccount = "TEST\\locked";
    public const string RemovedUserAccount = "TEST\\removed";
    public const string UnknownUserAccount = "TEST\\unknown";

    private const string LocalDbServer = "(localdb)\\MSSQLLocalDB";
    private readonly string _databaseName = $"IndependentApprovalTests_{Guid.NewGuid():N}";
    private readonly string _documentStoragePath = Path.Combine(
        Path.GetTempPath(),
        $"IndependentApprovalTests_{Guid.NewGuid():N}",
        "documents");

    private IndependentApprovalApiFactory? _factory;

    public Guid BootstrapAdministratorId { get; } = Guid.NewGuid();

    public string ConnectionString =>
        $"Server={LocalDbServer};Database={_databaseName};Integrated Security=True;" +
        "TrustServerCertificate=True;MultipleActiveResultSets=False;Pooling=False;Connect Timeout=30";

    public async Task InitializeAsync()
    {
        EnsureDisposableLocalDbTarget();
        _factory = new IndependentApprovalApiFactory(ConnectionString, _documentStoragePath);

        try
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<IndependentApprovalDbContext>();
            EnsureResolvedContextUsesDisposableLocalDb(dbContext);
            await dbContext.Database.MigrateAsync();
            await SeedApplicationAccessAsync(dbContext);
        }
        catch
        {
            try
            {
                _factory.Dispose();
            }
            finally
            {
                try
                {
                    await DeleteDatabaseAsync();
                }
                finally
                {
                    DeleteDocumentStorage();
                }
            }

            throw;
        }
    }

    public HttpClient CreateClient(string accountName)
    {
        var factory = _factory
            ?? throw new InvalidOperationException("The API fixture has not been initialized.");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.AccountHeaderName,
            accountName);
        return client;
    }

    public async Task DisposeAsync()
    {
        try
        {
            _factory?.Dispose();
        }
        finally
        {
            try
            {
                await DeleteDatabaseAsync();
            }
            finally
            {
                DeleteDocumentStorage();
            }
        }
    }

    private async Task SeedApplicationAccessAsync(
        IndependentApprovalDbContext dbContext)
    {
        var now = DateTimeOffset.UtcNow;
        var accessRoleId = Guid.NewGuid();
        var users = new[]
        {
            CreateUser(
                BootstrapAdministratorId,
                BootstrapAdministratorAccount,
                1101,
                "Bootstrap Administrator",
                now),
            CreateUser(
                Guid.NewGuid(),
                ApplicationUserAccount,
                1102,
                "Application Member",
                now),
            CreateUser(
                Guid.NewGuid(),
                LockedUserAccount,
                1103,
                "Locked Member",
                now,
                isLocked: true),
            CreateUser(
                Guid.NewGuid(),
                RemovedUserAccount,
                1104,
                "Removed Member",
                now,
                isRemoved: true)
        };
        var accessRole = new ApplicationRole
        {
            Id = accessRoleId,
            Code = "APPLICATION_USER",
            NormalizedCode = "APPLICATION_USER",
            NameEnglish = "Application user",
            NameArabic = "مستخدم التطبيق",
            DescriptionEnglish = "Grants checkpoint test application access.",
            DescriptionArabic = "يمنح صلاحية الوصول لاختبارات التطبيق.",
            IsActive = true,
            IsArchived = false,
            CreatedAtUtc = now,
            CreatedByAccount = BootstrapAdministratorAccount,
            CreatedByUserId = BootstrapAdministratorId
        };

        dbContext.ApplicationUsers.AddRange(users);
        dbContext.ApplicationRoles.Add(accessRole);
        dbContext.RolePermissions.Add(new RolePermission
        {
            Id = Guid.NewGuid(),
            ApplicationRoleId = accessRoleId,
            PermissionId = PermissionCatalog.AccessApplicationId,
            GrantedAtUtc = now,
            GrantedByAccount = BootstrapAdministratorAccount,
            GrantedByUserId = BootstrapAdministratorId
        });

        foreach (var user in users.Where(user => user.Id != BootstrapAdministratorId))
        {
            dbContext.ApplicationUserRoles.Add(new ApplicationUserRole
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                ApplicationRoleId = accessRoleId,
                AssignedAtUtc = now,
                AssignedByAccount = BootstrapAdministratorAccount,
                AssignedByUserId = BootstrapAdministratorId
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static ApplicationUser CreateUser(
        Guid id,
        string accountName,
        int sidRid,
        string displayName,
        DateTimeOffset timestamp,
        bool isLocked = false,
        bool isRemoved = false)
    {
        var separatorIndex = accountName.IndexOf('\\');
        var domain = accountName[..separatorIndex];
        var userName = accountName[(separatorIndex + 1)..];

        return new ApplicationUser
        {
            Id = id,
            AdSid = CreateBinarySid(sidRid),
            AdObjectGuid = Guid.NewGuid(),
            AccountName = accountName,
            Domain = domain,
            SamAccountName = userName,
            UserPrincipalName = $"{userName}@test.local",
            NormalizedAccountName = accountName.ToUpperInvariant(),
            DisplayName = displayName,
            Email = $"{userName}@test.local",
            IsActive = true,
            IsLocked = isLocked,
            LockReason = isLocked ? "Locked by the checkpoint test fixture." : null,
            IsRemoved = isRemoved,
            CreatedAtUtc = timestamp,
            CreatedByAccount = BootstrapAdministratorAccount,
            LockedAtUtc = isLocked ? timestamp : null,
            LockedByAccount = isLocked ? BootstrapAdministratorAccount : null,
            RemovedAtUtc = isRemoved ? timestamp : null,
            RemovedByAccount = isRemoved ? BootstrapAdministratorAccount : null
        };
    }

    private static byte[] CreateBinarySid(int rid)
    {
        // Binary representation of S-1-5-21-111111111-222222222-333333333-{rid}.
        var sid = new byte[28];
        sid[0] = 1;
        sid[1] = 5;
        sid[7] = 5;
        BinaryPrimitives.WriteUInt32LittleEndian(sid.AsSpan(8), 21);
        BinaryPrimitives.WriteUInt32LittleEndian(sid.AsSpan(12), 111111111);
        BinaryPrimitives.WriteUInt32LittleEndian(sid.AsSpan(16), 222222222);
        BinaryPrimitives.WriteUInt32LittleEndian(sid.AsSpan(20), 333333333);
        BinaryPrimitives.WriteUInt32LittleEndian(sid.AsSpan(24), (uint)rid);
        return sid;
    }

    private async Task DeleteDatabaseAsync()
    {
        EnsureDisposableLocalDbTarget();
        var options = new DbContextOptionsBuilder<IndependentApprovalDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var dbContext = new IndependentApprovalDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
    }

    private void EnsureDisposableLocalDbTarget()
    {
        if (!ConnectionString.Contains(LocalDbServer, StringComparison.OrdinalIgnoreCase)
            || ConnectionString.Contains("SPSE26H", StringComparison.OrdinalIgnoreCase)
            || !_databaseName.StartsWith(
                "IndependentApprovalTests_",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Checkpoint tests may only target their unique SQL Server LocalDB database.");
        }
    }

    private void EnsureResolvedContextUsesDisposableLocalDb(
        IndependentApprovalDbContext dbContext)
    {
        var resolvedConnectionString = dbContext.Database
            .GetDbConnection()
            .ConnectionString;

        if (!resolvedConnectionString.Contains(
                LocalDbServer,
                StringComparison.OrdinalIgnoreCase)
            || !resolvedConnectionString.Contains(
                _databaseName,
                StringComparison.OrdinalIgnoreCase)
            || resolvedConnectionString.Contains(
                "SPSE26H",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The resolved API DbContext is not using the disposable LocalDB database.");
        }
    }

    private void DeleteDocumentStorage()
    {
        if (!Directory.Exists(_documentStoragePath))
        {
            return;
        }

        var disposableRoot = Path.GetDirectoryName(_documentStoragePath)
            ?? throw new InvalidOperationException("The test storage root is invalid.");

        if (!Path.GetFileName(disposableRoot).StartsWith(
                "IndependentApprovalTests_",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Checkpoint test storage cleanup refused an unexpected path.");
        }

        Directory.Delete(disposableRoot, recursive: true);
    }
}
