namespace IndependentApproval.Api.Contracts.Auth;

public sealed record CurrentUserResponse(
    bool IsAuthenticated,
    string AccountName,
    string Domain,
    string UserName,
    string AuthenticationType);
