using IndependentApproval.Api.Application.Administration;
using IndependentApproval.Api.Application.Authorization;
using IndependentApproval.Api.Application.Documents;
using IndependentApproval.Api.Infrastructure;
using IndependentApproval.Api.Infrastructure.Persistence;
using IndependentApproval.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

const string DevClientCorsPolicy = "DevClient";
const string DevClientOriginsConfigurationKey = "Cors:DevClient:AllowedOrigins";
const string DatabaseConnectionStringName = "IndependentApprovalDatabase";

var builder = WebApplication.CreateBuilder(args);

var databaseConnectionString = builder.Configuration.GetConnectionString(DatabaseConnectionStringName)
    ?? throw new InvalidOperationException(
        $"Connection string '{DatabaseConnectionStringName}' is not configured.");

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AntiforgeryValidationProblemDetailsFilter());
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressReadingTokenFromFormBody = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = builder.Environment.IsDevelopment()
        ? SameSiteMode.None
        : SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
});
builder.Services
    .AddOptions<DocumentStorageOptions>()
    .BindConfiguration(DocumentStorageOptions.SectionName)
    .PostConfigure(options => options.NormalizeAllowedExtensions())
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<DocumentStorageOptions>, DocumentStorageOptionsValidator>();
builder.Services
    .AddOptions<ApplicationAuthorizationOptions>()
    .BindConfiguration(ApplicationAuthorizationOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<
    IValidateOptions<ApplicationAuthorizationOptions>,
    ApplicationAuthorizationOptionsValidator>();
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = DocumentStorageOptions.MultipartBodyLengthLimitBytes);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IDocumentStorage, FileSystemDocumentStorage>();
builder.Services.AddHostedService<DevelopmentDocumentStorageInitializer>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IApplicationAccessService, ApplicationAccessService>();
builder.Services.AddScoped<IAdministrationActorAccessor, AdministrationActorAccessor>();
builder.Services.AddScoped<
    IApplicationUserAdministrationService,
    ApplicationUserAdministrationService>();
builder.Services.AddScoped<IRoleAdministrationService, RoleAdministrationService>();
builder.Services.AddScoped<IAdministrationSummaryService, AdministrationSummaryService>();
builder.Services.AddDbContext<IndependentApprovalDbContext>(options =>
    options.UseSqlServer(
        databaseConnectionString,
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));
builder.Services
    .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
builder.Services.AddScoped<IAuthorizationHandler, ApplicationUserAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SystemAdministratorAuthorizationHandler>();
builder.Services.AddSingleton<
    IAuthorizationMiddlewareResultHandler,
    AuthorizationProblemDetailsResultHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicyNames.ApplicationUser, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new ApplicationUserRequirement());
    });
    options.AddPolicy(AuthorizationPolicyNames.SystemAdministrator, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new SystemAdministratorRequirement());
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AdministrationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

if (builder.Environment.IsDevelopment())
{
    var allowedOrigins = builder.Configuration
        .GetSection(DevClientOriginsConfigurationKey)
        .GetChildren()
        .Select(section => section.Value)
        .OfType<string>()
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .ToArray();

    if (allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException(
            $"No development client origins are configured at '{DevClientOriginsConfigurationKey}'.");
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(DevClientCorsPolicy, policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Services.AddOpenApi();
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevClientCorsPolicy);
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapFallbackToFile(
    "{*path:regex(^(?!(api|health)($|/)).*):nonfile}",
    "index.html");

app.Run();

public partial class Program;
