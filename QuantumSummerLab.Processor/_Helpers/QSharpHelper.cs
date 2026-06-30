using QsharpBridge;
using QuantumSummerLab.Processor.Extensions;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QuantumSummerLab.Processor.Helpers;

public interface IQSharpHelper
{
    QSharpFeedback Verify(QSharpRequest request);
}

public class QSharpHelper : IQSharpHelper
{
    private const int _shots = 1;

    public QSharpFeedback Verify(QSharpRequest request)
    {
        try
        {
            var verificationTemplate = request.VerificationTemplate.FromBase64String();
            var solution = request.Solution.FromBase64String();
            var expectedOutput = request.ExpectedOutput.FromBase64String();
            var expectedStates = request.ExpectedStates.FromBase64String();

            if (solution.Contains("while"))
            {
                return new QSharpFeedback
                {
                    IsValid = false,
                    Messages = [new QSharpFeedbackMessage { Valid = false, Message = "This challenge does not need a while-loop" }]
                };
            }

            var expectedDeserializedStates = string.IsNullOrEmpty(expectedStates) ? new List<QSharpState>() : JsonSerializer.Deserialize<List<QSharpState>>(expectedStates, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var qsharpSource = verificationTemplate.Replace("<<SOLVE>>", solution);

            var executionOptions = ExecutionOptions.FromShots(_shots);
            var resultShots = GlobalQsharpBridge.RunQsWithOptions(qsharpSource, executionOptions);

            var isValid = true;

            var feedbackMessages = new List<QSharpFeedbackMessage>();

            for (var i = 0; i < _shots; i++)
            {
                if (!string.IsNullOrEmpty(expectedOutput))
                {
                    var actualOutput = resultShots[i].result;
                    isValid = actualOutput == expectedOutput ? isValid : false;
                }

                if (resultShots[i].messages != null)
                {
                    foreach (var message in resultShots[i].messages)
                    {
                        var feedbackMessage = JsonSerializer.Deserialize<QSharpFeedbackMessage>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (feedbackMessage != null)
                        {
                            isValid = isValid && feedbackMessage.Valid;
                            feedbackMessages.Add(feedbackMessage);
                        }
                    }
                }

                if (resultShots[i].states != null)
                {
                    var actualStates = resultShots[i].states;

                    foreach (var expectedState in expectedDeserializedStates)
                    {
                        var actualState = actualStates.FirstOrDefault(s => s.id == expectedState.Id);
                        if (actualState != null)
                        {
                            var actualAmplitudeReal = Math.Round(actualState.amplitudeReal, 4);
                            var expectedAmplitudeReal = Math.Round(expectedState.AmplitudeReal, 4);
                            var actualAmplitudeImaginary = Math.Round(actualState.amplitudeImaginary, 4);
                            var expectedAmplitudeImaginary = Math.Round(expectedState.AmplitudeImaginary, 4);

                            var isAmplitudeValid = actualAmplitudeReal == expectedAmplitudeReal &&
                                                   actualAmplitudeImaginary == expectedAmplitudeImaginary;

                            var expectedAmplitudes = $"{expectedAmplitudeReal:F4} {(expectedAmplitudeImaginary >= 0 ? "+" : "-")} {Math.Abs(expectedAmplitudeImaginary):F4}𝑖";
                            var actualAmplitudes = $"{actualAmplitudeReal:F4} {(actualAmplitudeImaginary >= 0 ? "+" : "-")} {Math.Abs(actualAmplitudeImaginary):F4}𝑖";

                            if (!isAmplitudeValid)
                            {
                                isValid = false;
                                feedbackMessages.Add(new QSharpFeedbackMessage
                                {
                                    Valid = false,
                                    Message = $"Simulated quantum state {expectedState.Id} has an incorrect amplitude: Expected: {expectedAmplitudes}, Actual: {actualAmplitudes}"
                                });
                            }
                            else
                            {
                                feedbackMessages.Add(new QSharpFeedbackMessage
                                {
                                    Valid = true,
                                    Message = $"Expected simulated quantum state {expectedState.Id} was successfully encountered with amplitude {actualAmplitudes}"
                                });
                            }
                        }
                        else
                        {
                            isValid = false;
                            feedbackMessages.Add(new QSharpFeedbackMessage
                            {
                                Valid = false,
                                Message = $"Expected simulated quantum state {expectedState.Id} was not encountered."
                            });
                        }
                    }
                }
            }

            return new QSharpFeedback
            {
                IsValid = isValid,
                Messages = feedbackMessages
            };
        }
        catch (QsException ex)
        {
            return new QSharpFeedback
            {
                IsValid = false,
                Messages = [new QSharpFeedbackMessage
                {
                    Valid = false,
                    Message = ex.Message switch
                    {
                        string a when a.Contains("error: Compile") => "There has been an error compiling your Q# code!",
                        string b when b.Contains("error: Eval") => "There has been an error running your Q# code!",
                        _ => "There has been an unknown error :("
                    },
                    Details = BuildErrorDetails(ex.Message, request)
                }]
            };
        }
        catch
        {
            return new QSharpFeedback
            {
                IsValid = false,
                Messages = [new QSharpFeedbackMessage { Valid = false, Message = "There has been an unknown error :(" }]
            };
        }
    }

    private const int _maxReportedIssues = 12;

    // The native qsharp-bridge surfaces compilation errors as a Rust debug string
    // (e.g. NotFound("q", Span { lo: 2299, hi: 2300 })). These patterns turn the
    // most common variants into friendly messages and capture the byte offset (lo)
    // so we can resolve a line number in the team's own solution.
    private static readonly (Regex Pattern, Func<Match, (int Offset, string Description, bool ApproximateLine)> Describe)[] _errorPatterns =
    {
        (new Regex(@"MissingSemi\(Span \{ lo: (\d+), hi: \d+ \}\)", RegexOptions.Compiled),
            m => (int.Parse(m.Groups[1].Value), "Missing a semicolon ';'", false)),
        (new Regex(@"NotFoundQubit\(Span \{ lo: (\d+), hi: \d+ \}\)", RegexOptions.Compiled),
            m => (int.Parse(m.Groups[1].Value), "No qubit is in scope here (allocate one with 'use q = Qubit();')", false)),
        (new Regex(@"NotFound\(""([^""]*)"", Span \{ lo: (\d+), hi: \d+ \}\)", RegexOptions.Compiled),
            m => (int.Parse(m.Groups[2].Value), $"Unknown or undeclared identifier '{m.Groups[1].Value}'", false)),
        (new Regex(@"Token\(([^,]+), ([^,]+), Span \{ lo: (\d+), hi: \d+ \}\)", RegexOptions.Compiled),
            m => DescribeTokenMismatch(int.Parse(m.Groups[3].Value), m.Groups[1].Value, m.Groups[2].Value)),
    };

    // Strips the (very large) echoed source blocks from the raw Rust debug output.
    private static readonly Regex _sourcesRegex =
        new(@"sources: \[.*?\], error:", RegexOptions.Compiled | RegexOptions.Singleline);

    // Last-resort catch for any other single-span error variant (e.g. EmptyStmt) so the
    // user still gets a line number instead of the raw Rust debug output.
    private static readonly Regex _genericSpanRegex =
        new(@"(\w+)\(Span \{ lo: (\d+), hi: \d+ \}\)", RegexOptions.Compiled);

    private static string BuildErrorDetails(string rawMessage, QSharpRequest request)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return null;
        }

        var (solutionBytes, solutionByteOffset) = LocateSolution(request);

        var issues = new List<(int Offset, string Text)>();
        var seen = new HashSet<string>();
        var reportedOffsets = new HashSet<int>();

        void AddIssue(int offset, string description, bool approximateLine)
        {
            var location = DescribeLine(offset, solutionBytes, solutionByteOffset);
            if (location != null && approximateLine)
            {
                location += " (or the line above)";
            }
            var text = location != null ? $"{location}: {description}" : description;
            if (seen.Add(text))
            {
                issues.Add((offset, text));
            }
            reportedOffsets.Add(offset);
        }

        foreach (var (pattern, describe) in _errorPatterns)
        {
            foreach (Match match in pattern.Matches(rawMessage))
            {
                var (offset, description, approximateLine) = describe(match);
                AddIssue(offset, description, approximateLine);
            }
        }

        // Catch any remaining single-span variants we don't specifically recognise.
        foreach (Match match in _genericSpanRegex.Matches(rawMessage))
        {
            var offset = int.Parse(match.Groups[2].Value);
            if (reportedOffsets.Contains(offset))
            {
                continue;
            }

            AddIssue(offset, $"Compilation problem ({HumanizeVariant(match.Groups[1].Value)})", false);
        }

        if (issues.Count == 0)
        {
            // Unrecognised error shape: fall back to the de-noised raw output.
            return CleanErrorDetails(rawMessage);
        }

        var lines = issues
            .OrderBy(issue => issue.Offset)
            .Take(_maxReportedIssues)
            .Select(issue => $"• {issue.Text}")
            .ToList();

        if (issues.Count > _maxReportedIssues)
        {
            lines.Add($"• ...and {issues.Count - _maxReportedIssues} more issue(s).");
        }

        return string.Join(Environment.NewLine, lines);
    }

    // The team only authors the code that replaces the <<SOLVE>> placeholder, so we
    // recompose the harness to learn where their solution starts (as a UTF-8 byte
    // offset) and report line numbers relative to that, matching their editor.
    private static (byte[] SolutionBytes, int SolutionByteOffset) LocateSolution(QSharpRequest request)
    {
        try
        {
            var solution = request.Solution.FromBase64String();
            var template = request.VerificationTemplate.FromBase64String();
            var placeholderIndex = template.IndexOf("<<SOLVE>>", StringComparison.Ordinal);
            if (solution != null && placeholderIndex >= 0)
            {
                var solutionBytes = Encoding.UTF8.GetBytes(solution);
                var solutionByteOffset = Encoding.UTF8.GetByteCount(template.Substring(0, placeholderIndex));
                return (solutionBytes, solutionByteOffset);
            }
        }
        catch
        {
            // If we can't recompose the source we simply omit line numbers.
        }

        return (null, -1);
    }

    private static string DescribeLine(int globalByteOffset, byte[] solutionBytes, int solutionByteOffset)
    {
        if (solutionBytes == null || solutionByteOffset < 0)
        {
            return null;
        }

        var relative = globalByteOffset - solutionByteOffset;
        if (relative < 0 || relative > solutionBytes.Length)
        {
            // The error points into the verification harness, not the team's own code.
            return null;
        }

        var line = 1;
        for (var i = 0; i < relative; i++)
        {
            if (solutionBytes[i] == (byte)'\n')
            {
                line++;
            }
        }

        return $"Line {line}";
    }

    private static (int Offset, string Description, bool ApproximateLine) DescribeTokenMismatch(int offset, string expected, string found)
    {
        expected = expected.Trim();
        if (expected == "Semi")
        {
            // The parser flags the missing ';' at the *next* token, so the semicolon
            // itself usually belongs at the end of the previous (often non-blank) line.
            return (offset, "Missing a semicolon ';'", true);
        }

        return (offset, $"Expected {HumanizeToken(expected)} but found {HumanizeToken(found)}", false);
    }

    private static string HumanizeToken(string token)
    {
        token = token.Trim();
        return token switch
        {
            "Semi" => "a semicolon ';'",
            "Comma" => "a comma ','",
            "Colon" => "a colon ':'",
            "Eof" => "the end of the file",
            "Ident" => "an identifier",
            "Int" or "BigInt" or "Float" => "a number",
            "String" => "a string",
            "Open(Brace)" => "an opening brace '{'",
            "Close(Brace)" => "a closing brace '}'",
            "Open(Paren)" => "an opening parenthesis '('",
            "Close(Paren)" => "a closing parenthesis ')'",
            "Open(Bracket)" => "an opening bracket '['",
            "Close(Bracket)" => "a closing bracket ']'",
            _ => $"'{token}'"
        };
    }

    private static string HumanizeVariant(string variant)
    {
        // Split a CamelCase variant name into lower-case words, e.g. "EmptyStmt" -> "empty stmt".
        var spaced = Regex.Replace(variant, "(?<=[a-z0-9])(?=[A-Z])", " ");
        return spaced.ToLowerInvariant();
    }

    private static string CleanErrorDetails(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        // The QsException.ErrorMessage variant formats its message as "@errorText=<compiler output>".
        const string prefix = "@errorText=";
        if (message.StartsWith(prefix, StringComparison.Ordinal))
        {
            message = message.Substring(prefix.Length);
        }

        // Drop the echoed source blocks so the fallback stays readable.
        message = _sourcesRegex.Replace(message, "error:");

        return message.Trim();
    }
}

public class QSharpRequest
{
    public string VerificationTemplate { get; set; }
    public string Solution { get; set; }
    public string ExpectedOutput { get; set; }
    public string ExpectedStates { get; set; }
}

public class QSharpFeedback
{
    public bool IsValid { get; set; }
    public List<QSharpFeedbackMessage> Messages { get; set; } = new List<QSharpFeedbackMessage>();
}

public class QSharpFeedbackMessage
{
    public bool Valid { get; set; }
    public string Message { get; set; }
    public string Details { get; set; }
}

public class QSharpState
{
    public string Id { get; set; }
    public double AmplitudeReal { get; set; }
    public double AmplitudeImaginary { get; set; }
}