namespace IndependentApproval.Api.Contracts.Administration.Users;

public sealed record ApplicationUserConcurrencyRequest(string? RowVersion);
