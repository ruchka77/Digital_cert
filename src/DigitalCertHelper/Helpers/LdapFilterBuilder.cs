using System.Text;

namespace DigitalCertHelper.Helpers;

public static class LdapFilterBuilder
{
    private const string UserClause = "(objectCategory=person)(objectClass=user)";

    /// <summary>Each word must appear in a name attribute, so "Michael Berger" and "Berger Michael" both match.</summary>
    public static string BuildNameSearch(string term)
    {
        var words = term.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            throw new ArgumentException("The search term is empty.", nameof(term));
        }

        var filter = new StringBuilder("(&").Append(UserClause);
        foreach (var word in words)
        {
            var value = Escape(word);
            filter.Append($"(|(givenName=*{value}*)(sn=*{value}*)(displayName=*{value}*))");
        }

        return filter.Append(')').ToString();
    }

    public static string BuildObjectIdLookup(Guid objectId)
    {
        var bytes = string.Concat(objectId.ToByteArray().Select(b => $"\\{b:x2}"));
        return $"(&{UserClause}(objectGUID={bytes}))";
    }

    /// <summary>RFC 4515 escaping; prevents LDAP injection.</summary>
    public static string Escape(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            result.Append(c switch
            {
                '\\' => "\\5c",
                '*' => "\\2a",
                '(' => "\\28",
                ')' => "\\29",
                '\0' => "\\00",
                '/' => "\\2f",
                _ => c.ToString(),
            });
        }

        return result.ToString();
    }
}
