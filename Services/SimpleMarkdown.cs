using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace TicketSupport.Services;

public static partial class SimpleMarkdown
{
    public static string Render(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return "<p class=\"mb-0 text-muted\">Açıklama girilmedi.</p>";
        }

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var builder = new StringBuilder();
        var paragraph = new List<string>();
        var inList = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph(builder, paragraph);
                CloseList(builder, ref inList);
                continue;
            }

            if (TryRenderHeading(builder, paragraph, ref inList, line))
            {
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                FlushParagraph(builder, paragraph);
                if (!inList)
                {
                    builder.Append("<ul>");
                    inList = true;
                }

                builder.Append("<li>")
                    .Append(ApplyInlineFormatting(WebUtility.HtmlEncode(line[2..].Trim())))
                    .Append("</li>");
                continue;
            }

            CloseList(builder, ref inList);
            paragraph.Add(line.Trim());
        }

        FlushParagraph(builder, paragraph);
        CloseList(builder, ref inList);
        return builder.ToString();
    }

    private static bool TryRenderHeading(StringBuilder builder, List<string> paragraph, ref bool inList, string line)
    {
        if (!line.StartsWith('#'))
        {
            return false;
        }

        var level = line.TakeWhile(ch => ch == '#').Count();
        if (level is < 1 or > 3 || line.Length <= level || line[level] != ' ')
        {
            return false;
        }

        FlushParagraph(builder, paragraph);
        CloseList(builder, ref inList);
        builder.Append($"<h{level}>")
            .Append(ApplyInlineFormatting(WebUtility.HtmlEncode(line[(level + 1)..].Trim())))
            .Append($"</h{level}>");
        return true;
    }

    private static void FlushParagraph(StringBuilder builder, List<string> paragraph)
    {
        if (paragraph.Count == 0)
        {
            return;
        }

        var content = string.Join("<br />", paragraph.Select(line => ApplyInlineFormatting(WebUtility.HtmlEncode(line))));
        builder.Append("<p>").Append(content).Append("</p>");
        paragraph.Clear();
    }

    private static void CloseList(StringBuilder builder, ref bool inList)
    {
        if (!inList)
        {
            return;
        }

        builder.Append("</ul>");
        inList = false;
    }

    private static string ApplyInlineFormatting(string encodedText)
    {
        var bolded = BoldRegex().Replace(encodedText, "<strong>$1</strong>");
        return CodeRegex().Replace(bolded, "<code>$1</code>");
    }

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"`(.+?)`")]
    private static partial Regex CodeRegex();
}
