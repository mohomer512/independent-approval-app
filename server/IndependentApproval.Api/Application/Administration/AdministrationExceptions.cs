namespace IndependentApproval.Api.Application.Administration;

public abstract class AdministrationException(
    int statusCode,
    string title,
    string code,
    string detail) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;

    public string Title { get; } = title;

    public string Code { get; } = code;
}

public sealed class AdministrationNotFoundException(string detail) :
    AdministrationException(
        StatusCodes.Status404NotFound,
        "The requested administration resource was not found.",
        "administration.not_found",
        detail);

public sealed class AdministrationConflictException(string code, string detail) :
    AdministrationException(
        StatusCodes.Status409Conflict,
        "The administration change could not be completed.",
        code,
        detail);

public sealed class AdministrationBadRequestException(string code, string detail) :
    AdministrationException(
        StatusCodes.Status400BadRequest,
        "The administration request is invalid.",
        code,
        detail);

public sealed class AdministrationServiceUnavailableException(string code, string detail) :
    AdministrationException(
        StatusCodes.Status503ServiceUnavailable,
        "A required administration service is unavailable.",
        code,
        detail);

public sealed class AdministrationValidationException(
    IReadOnlyDictionary<string, string[]> errors) :
    AdministrationException(
        StatusCodes.Status400BadRequest,
        "One or more validation errors occurred.",
        "administration.validation_failed",
        "Correct the validation errors and try again.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public static AdministrationValidationException For(
        string field,
        string message) =>
        new(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [field] = [message]
        });
}
