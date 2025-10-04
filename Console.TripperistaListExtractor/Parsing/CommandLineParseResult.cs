using Core.TripperistaListExtractor.Options;

namespace Console.TripperistaListExtractor.Parsing;

/// <summary>
/// Represents the outcome of parsing command line arguments.
/// </summary>
public sealed class CommandLineParseResult
{
    /// <summary>
    /// Gets a value indicating whether the user requested help text.
    /// </summary>
    public bool ShowHelp { get; init; }

    /// <summary>
    /// Gets the diagnostic error message if parsing failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the successfully parsed options when <see cref="ErrorMessage"/> is <c>null</c>.
    /// </summary>
    public ExtractionOptions? Options { get; init; }
}
