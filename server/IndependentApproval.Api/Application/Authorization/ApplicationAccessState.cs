namespace IndependentApproval.Api.Application.Authorization;

public static class ApplicationAccessStates
{
    public const string Granted = "granted";
    public const string Unknown = "unknown";
    public const string Inactive = "inactive";
    public const string Locked = "locked";
    public const string Removed = "removed";
    public const string MissingAccessPermission = "missingAccessPermission";
}

public sealed record ApplicationAccessSnapshot(
    Guid? ApplicationUserId,
    string AccountName,
    string Domain,
    string UserName,
    string DisplayName,
    string AccessState,
    bool HasApplicationAccess,
    bool IsSystemAdministrator,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<string> Permissions);
