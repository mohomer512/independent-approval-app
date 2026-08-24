namespace IndependentApproval.Api.Application.Authorization;

public sealed class ApplicationAuthorizationOptions
{
    public const string SectionName = "Authorization";

    public string[] SystemAdministratorAccounts { get; set; } = [];
}
