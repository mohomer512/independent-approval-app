using System.Text;

namespace IndependentApproval.Api.Infrastructure.ActiveDirectory;

public static class LdapFilterEscaper
{
    public static string EscapeAssertionValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            switch (character)
            {
                case '\0':
                    builder.Append("\\00");
                    break;
                case '(':
                    builder.Append("\\28");
                    break;
                case ')':
                    builder.Append("\\29");
                    break;
                case '*':
                    builder.Append("\\2a");
                    break;
                case '\\':
                    builder.Append("\\5c");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }

    public static string EscapeBinary(ReadOnlySpan<byte> value)
    {
        var builder = new StringBuilder(value.Length * 3);

        foreach (var item in value)
        {
            builder.Append('\\');
            builder.Append(item.ToString("X2"));
        }

        return builder.ToString();
    }
}
