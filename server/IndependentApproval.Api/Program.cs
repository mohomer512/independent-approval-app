using IndependentApproval.Api.Infrastructure;
using IndependentApproval.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;

const string DevClientCorsPolicy = "DevClient";
const string DevClientOriginsConfigurationKey = "Cors:DevClient:AllowedOrigins";
const string DatabaseConnectionStringName = "IndependentApprovalDatabase";

var builder = WebApplication.CreateBuilder(args);

var databaseConnectionString = builder.Configuration.GetConnectionString(DatabaseConnectionStringName)
    ?? throw new InvalidOperationException(
        $"Connection string '{DatabaseConnectionStringName}' is not configured.");

builder.Services.AddControllers();
builder.Services.AddDbContext<IndependentApprovalDbContext>(options =>
    options.UseSqlServer(
        databaseConnectionString,
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));
builder.Services
    .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
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
