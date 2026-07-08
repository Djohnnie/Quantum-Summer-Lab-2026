using System.Text;
using System.Text.RegularExpressions;

namespace QuantumSummerLab.Web.Components;

/// <summary>
/// Splits a raw Qubit Buddy chat message into the ordered sequence of chat bubbles that should
/// be rendered for it.
///
/// Qubit Buddy is instructed (see CopilotHelper.BuildInstructions) to separate every sentence
/// with "[BR]" and to wrap any Q# code in a ```-fenced block, but LLM output is not 100%
/// reliable, so this formatter never depends on that alone:
///  - ```-fenced code (if present) is always kept together, verbatim, in its own bubble.
///  - Q# code without fences (e.g. a bare "operation ... { ... }" snippet) is still detected
///    heuristically and re-indented, so it still gets its own, readable bubble.
///  - Regular prose is split on "[BR]" when present; if a chunk of text has no "[BR]" at all,
///    it is still broken up on sentence boundaries and bullet markers so the UI never collapses
///    into a single wall of text.
/// </summary>
public static class ChatMessageFormatter
{
    private static readonly Regex FencedCodeBlockRegex = new(@"```(?:[^\r\n`]*\r?\n)?(.*?)```", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex SentenceBoundaryRegex = new(@"(?<=[.!?])\s+(?=\S)", RegexOptions.Compiled);
    private static readonly string[] CodeStartKeywords = ["operation ", "function ", "namespace "];

    public sealed record Segment(string Text, bool IsCode);

    public static IReadOnlyList<Segment> Parse(string? content)
    {
        var segments = new List<Segment>();

        if (string.IsNullOrWhiteSpace(content))
        {
            return segments;
        }

        var lastIndex = 0;

        foreach (Match match in FencedCodeBlockRegex.Matches(content))
        {
            if (match.Index > lastIndex)
            {
                AddTextOrInlineCodeSegments(segments, content[lastIndex..match.Index]);
            }

            var code = match.Groups[1].Value.Trim('\r', '\n');
            if (!string.IsNullOrWhiteSpace(code))
            {
                segments.Add(new Segment(code, true));
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < content.Length)
        {
            AddTextOrInlineCodeSegments(segments, content[lastIndex..]);
        }

        return segments;
    }

    /// <summary>
    /// Handles a chunk of content that is known to be outside of any ```-fenced code block.
    /// Detects and pulls out any un-fenced Q# code snippet it can find, and treats the rest as
    /// prose.
    /// </summary>
    private static void AddTextOrInlineCodeSegments(List<Segment> segments, string text)
    {
        var remaining = text;

        while (TryExtractInlineCode(remaining, out var before, out var code, out var after))
        {
            AddTextSegments(segments, before);
            segments.Add(new Segment(ReindentCode(code), true));
            remaining = after;
        }

        AddTextSegments(segments, remaining);
    }

    /// <summary>
    /// Looks for a Q# code snippet that was NOT wrapped in ```-fences, starting at one of the
    /// well-known Q# declaration keywords and spanning to the matching closing brace.
    /// </summary>
    private static bool TryExtractInlineCode(string text, out string before, out string code, out string after)
    {
        foreach (var keyword in CodeStartKeywords)
        {
            var start = text.IndexOf(keyword, StringComparison.Ordinal);
            if (start < 0)
            {
                continue;
            }

            var braceStart = text.IndexOf('{', start);
            if (braceStart < 0)
            {
                continue;
            }

            var depth = 0;
            var end = -1;

            for (var i = braceStart; i < text.Length; i++)
            {
                if (text[i] == '{')
                {
                    depth++;
                }
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        end = i;
                        break;
                    }
                }
            }

            if (end < 0)
            {
                continue;
            }

            before = text[..start];
            code = text[start..(end + 1)];
            after = text[(end + 1)..];
            return true;
        }

        before = string.Empty;
        code = string.Empty;
        after = string.Empty;
        return false;
    }

    /// <summary>
    /// Re-indents a Q# snippet that may have arrived as a single line (or with inconsistent
    /// whitespace), using brace depth to decide indentation.
    /// </summary>
    private static string ReindentCode(string code)
    {
        var collapsed = Regex.Replace(code.Trim(), @"\s+", " ");
        var sb = new StringBuilder();
        var indent = 0;

        void TrimTrailingWhitespace()
        {
            while (sb.Length > 0 && (sb[^1] == ' ' || sb[^1] == '\n'))
            {
                sb.Length--;
            }
        }

        void NewLine()
        {
            sb.Append('\n');
            sb.Append(' ', indent * 4);
        }

        for (var i = 0; i < collapsed.Length; i++)
        {
            var c = collapsed[i];

            if (c == '{')
            {
                TrimTrailingWhitespace();
                sb.Append(" {");
                indent++;
                NewLine();
                while (i + 1 < collapsed.Length && collapsed[i + 1] == ' ')
                {
                    i++;
                }
            }
            else if (c == '}')
            {
                TrimTrailingWhitespace();
                indent = Math.Max(0, indent - 1);
                NewLine();
                sb.Append('}');
                while (i + 1 < collapsed.Length && collapsed[i + 1] == ' ')
                {
                    i++;
                }

                if (i + 1 < collapsed.Length && collapsed[i + 1] != ';')
                {
                    NewLine();
                }
            }
            else if (c == ';')
            {
                sb.Append(';');
                NewLine();
                while (i + 1 < collapsed.Length && collapsed[i + 1] == ' ')
                {
                    i++;
                }
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Trim();
    }

    private static void AddTextSegments(List<Segment> segments, string text)
    {
        var normalized = text.Replace("\r\n", "\n");

        IEnumerable<string> pieces;

        if (normalized.Contains("[BR]"))
        {
            // Preferred path: Qubit Buddy followed the [BR]-per-sentence convention.
            pieces = normalized.Split("[BR]", StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            // Fallback so the UI still reads as separate bubbles even if the model forgets to
            // add [BR]: break on bullet markers and sentence boundaries.
            var withBulletBreaks = normalized.Replace("●", "\n●");
            pieces = withBulletBreaks
                .Split('\n')
                .SelectMany(line => SentenceBoundaryRegex.Split(line));
        }

        foreach (var piece in pieces)
        {
            var trimmed = piece.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                segments.Add(new Segment(trimmed, false));
            }
        }
    }
}
