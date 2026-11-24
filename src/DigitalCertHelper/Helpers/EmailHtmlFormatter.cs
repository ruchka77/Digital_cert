using System.Net;

namespace DigitalCertHelper.Helpers;

public static class EmailHtmlFormatter
{
    /// <summary>Wraps plain text as right-to-left HTML without changing the visible text.</summary>
    public static string ToRightToLeftHtml(string plainText)
    {
        var lines = plainText.ReplaceLineEndings("\n").Split('\n').Select(WebUtility.HtmlEncode);
        return "<html><head><meta charset=\"utf-8\"></head><body dir=\"rtl\">"
            + "<p dir=\"rtl\" style=\"text-align:right;direction:rtl;font-family:Arial,sans-serif;font-size:11pt\">"
            + string.Join("<br>", lines)
            + "</p></body></html>";
    }
}
