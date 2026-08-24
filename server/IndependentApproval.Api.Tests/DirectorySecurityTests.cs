using IndependentApproval.Api.Application.Directory;
using IndependentApproval.Api.Infrastructure.ActiveDirectory;

namespace IndependentApproval.Api.Tests;

public sealed class DirectorySecurityTests
{
    private static readonly DirectoryOptions SearchOptions = new()
    {
        MinimumSearchLength = 2,
        MaximumSearchLength = 100,
        MaximumPageSize = 50,
        MaximumPage = 10,
        MaximumResultCount = 250
    };

    [Fact]
    public void Ldap_assertion_value_escapes_every_rfc4515_metacharacter()
    {
        var escaped = LdapFilterEscaper.EscapeAssertionValue("a*b(c)\\\0");

        Assert.Equal("a\\2ab\\28c\\29\\5c\\00", escaped);
    }

    [Fact]
    public void Ldap_assertion_value_preserves_safe_unicode_text()
    {
        var escaped = LdapFilterEscaper.EscapeAssertionValue("آمنة Alice 123");

        Assert.Equal("آمنة Alice 123", escaped);
    }

    [Fact]
    public void Ldap_binary_value_escapes_every_byte()
    {
        var escaped = LdapFilterEscaper.EscapeBinary(
            new byte[] { 0x00, 0x01, 0xAB, 0xFF });

        Assert.Equal("\\00\\01\\AB\\FF", escaped);
    }

    [Fact]
    public void Directory_search_validation_trims_valid_bounded_query()
    {
        var result = DirectorySearchValidator.ValidateAndNormalize(
            "  Alice  ",
            page: 5,
            pageSize: 50,
            SearchOptions);

        Assert.Equal("Alice", result);
    }

    [Theory]
    [InlineData(" ", 1, 25, "query")]
    [InlineData("x", 1, 25, "query")]
    [InlineData("valid", 0, 25, "page")]
    [InlineData("valid", 11, 25, "page")]
    [InlineData("valid", 1, 0, "pageSize")]
    [InlineData("valid", 1, 51, "pageSize")]
    [InlineData("valid", 6, 50, "page")]
    public void Directory_search_validation_rejects_unbounded_requests(
        string query,
        int page,
        int pageSize,
        string expectedField)
    {
        var exception = Assert.Throws<DirectorySearchValidationException>(() =>
            DirectorySearchValidator.ValidateAndNormalize(
                query,
                page,
                pageSize,
                SearchOptions));

        Assert.Equal(expectedField, exception.Field);
    }
}
