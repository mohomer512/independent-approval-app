using System.Security.Cryptography;
using System.Text.Json;
using IndependentApproval.Api.Application.Directory;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace IndependentApproval.Api.Infrastructure.ActiveDirectory;

public sealed class DirectorySelectionTokenService : IDirectorySelectionTokenService
{
    private const string Purpose =
        "IndependentApproval.DirectorySelection.v1";

    private readonly ITimeLimitedDataProtector _protector;
    private readonly TimeSpan _lifetime;

    public DirectorySelectionTokenService(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<DirectoryOptions> options)
    {
        _protector = dataProtectionProvider
            .CreateProtector(Purpose)
            .ToTimeLimitedDataProtector();
        _lifetime = TimeSpan.FromMinutes(
            options.Value.SelectionTokenLifetimeMinutes);
    }

    public string Create(DirectoryUser directoryUser)
    {
        ArgumentNullException.ThrowIfNull(directoryUser);
        var payload = JsonSerializer.Serialize(new TokenPayload(
            Convert.ToBase64String(directoryUser.Sid),
            directoryUser.ObjectGuid));
        return _protector.Protect(payload, _lifetime);
    }

    public bool TryRead(
        string? selectionToken,
        out DirectorySelectionReference? reference)
    {
        reference = null;

        if (string.IsNullOrWhiteSpace(selectionToken)
            || selectionToken.Length > 4096)
        {
            return false;
        }

        try
        {
            var payloadJson = _protector.Unprotect(selectionToken, out _);
            var payload = JsonSerializer.Deserialize<TokenPayload>(payloadJson);

            if (payload is null || string.IsNullOrWhiteSpace(payload.Sid))
            {
                return false;
            }

            var sid = Convert.FromBase64String(payload.Sid);

            if (sid.Length is < 1 or > 68)
            {
                return false;
            }

            reference = new DirectorySelectionReference(sid, payload.ObjectGuid);
            return true;
        }
        catch (Exception exception) when (exception is CryptographicException
                                          or FormatException
                                          or JsonException)
        {
            return false;
        }
    }

    private sealed record TokenPayload(string Sid, Guid? ObjectGuid);
}
