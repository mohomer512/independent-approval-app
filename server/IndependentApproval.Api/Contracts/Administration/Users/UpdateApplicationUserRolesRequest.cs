namespace IndependentApproval.Api.Contracts.Administration.Users;

public sealed record UpdateApplicationUserRolesRequest(
    IReadOnlyList<Guid>? RoleIds,
    string? RowVersion);
