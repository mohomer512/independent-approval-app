namespace IndependentApproval.Api.Contracts.Administration.Users;

public sealed record LockApplicationUserRequest(
    string? Reason,
    string? RowVersion);
