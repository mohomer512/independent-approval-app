namespace IndependentApproval.Api.Application.Directory;

public sealed class DirectoryServiceUnavailableException() :
    Exception("The configured directory service is unavailable.");
