namespace IndependentApproval.Api.Contracts.Administration.Users;

public sealed record AddApplicationUserRequest(
    string? SelectionToken,
    IReadOnlyList<Guid>? RoleIds);
