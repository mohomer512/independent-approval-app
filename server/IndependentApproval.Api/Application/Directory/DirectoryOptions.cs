namespace IndependentApproval.Api.Application.Directory;

public sealed class DirectoryOptions
{
    public const string SectionName = "Directory";

    public string Server { get; set; } = string.Empty;

    public int Port { get; set; } = 389;

    public bool UseSsl { get; set; }

    public string DomainName { get; set; } = string.Empty;

    public string NetBiosDomainName { get; set; } = string.Empty;

    public string SearchBase { get; set; } = string.Empty;

    public int MinimumSearchLength { get; set; } = 2;

    public int MaximumSearchLength { get; set; } = 100;

    public int DefaultPageSize { get; set; } = 25;

    public int MaximumPageSize { get; set; } = 50;

    public int MaximumPage { get; set; } = 10;

    public int MaximumResultCount { get; set; } = 250;

    public int SearchTimeoutSeconds { get; set; } = 10;

    public int SelectionTokenLifetimeMinutes { get; set; } = 10;

    public string? CredentialUserName { get; set; }

    public string? CredentialPassword { get; set; }

    public string? CredentialDomain { get; set; }
}
