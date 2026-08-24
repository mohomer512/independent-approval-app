namespace IndependentApproval.Api.Application.Administration;

public static class AdministrationEncoding
{
    private const int SqlServerRowVersionLength = 8;

    public static string EncodeRowVersion(byte[] rowVersion) =>
        Convert.ToBase64String(rowVersion);

    public static byte[] DecodeRowVersion(string? encodedRowVersion)
    {
        if (string.IsNullOrWhiteSpace(encodedRowVersion))
        {
            throw AdministrationValidationException.For(
                "rowVersion",
                "A row version is required.");
        }

        try
        {
            var rowVersion = Convert.FromBase64String(encodedRowVersion);

            if (rowVersion.Length != SqlServerRowVersionLength)
            {
                throw AdministrationValidationException.For(
                    "rowVersion",
                    "The row version is invalid.");
            }

            return rowVersion;
        }
        catch (FormatException)
        {
            throw AdministrationValidationException.For(
                "rowVersion",
                "The row version is invalid.");
        }
    }

    public static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
        {
            throw AdministrationValidationException.For(
                "page",
                "Page must be greater than or equal to 1.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw AdministrationValidationException.For(
                "pageSize",
                "Page size must be between 1 and 100.");
        }
    }
}
