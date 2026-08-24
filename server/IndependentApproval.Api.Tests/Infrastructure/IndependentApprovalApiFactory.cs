using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using IndependentApproval.Api.Application.Directory;
using IndependentApproval.Api.Infrastructure.Persistence;

namespace IndependentApproval.Api.Tests.Infrastructure;

internal sealed class IndependentApprovalApiFactory(
    string connectionString,
    string documentStoragePath,
    FakeDirectoryService directoryService) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IndependentApprovalDatabase"] = connectionString,
                ["Authorization:SystemAdministratorAccounts:0"] =
                    AdministrationApiFixture.BootstrapAdministratorAccount,
                ["DocumentStorage:RootPath"] = documentStoragePath,
                ["DocumentStorage:MaxFileSizeBytes"] = (1024 * 1024).ToString(),
                ["DocumentStorage:AllowedExtensions:0"] = ".pdf",
                ["Directory:Server"] = "never-contact.example.invalid",
                ["Directory:Port"] = "389",
                ["Directory:UseSsl"] = "false",
                ["Directory:DomainName"] = "TEST.local",
                ["Directory:NetBiosDomainName"] = "TEST",
                ["Directory:SearchBase"] = "DC=TEST,DC=local",
                ["Directory:MinimumSearchLength"] = "2",
                ["Directory:MaximumSearchLength"] = "100",
                ["Directory:DefaultPageSize"] = "25",
                ["Directory:MaximumPageSize"] = "50",
                ["Directory:MaximumPage"] = "10",
                ["Directory:MaximumResultCount"] = "250",
                ["Directory:SearchTimeoutSeconds"] = "1",
                ["Directory:SelectionTokenLifetimeMinutes"] = "10"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<
                IDbContextOptionsConfiguration<IndependentApprovalDbContext>>();
            services.RemoveAll<DbContextOptions<IndependentApprovalDbContext>>();
            services.RemoveAll<IndependentApprovalDbContext>();
            services.AddDbContext<IndependentApprovalDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.RemoveAll<IDirectoryService>();
            services.AddSingleton<IDirectoryService>(directoryService);

            services.RemoveAll<IDataProtectionProvider>();
            services.AddSingleton<IDataProtectionProvider>(
                new EphemeralDataProtectionProvider());

            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme =
                        TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultForbidScheme =
                        TestAuthenticationHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme,
                    _ => { });
        });
    }
}
