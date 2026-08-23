using Microsoft.Extensions.Options;

namespace IndependentApproval.Api.Infrastructure.Storage;

public sealed class DevelopmentDocumentStorageInitializer(
    IHostEnvironment hostEnvironment,
    IOptions<DocumentStorageOptions> options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (hostEnvironment.IsDevelopment())
        {
            Directory.CreateDirectory(options.Value.RootPath);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
