using Core.TripperistaListExtractor.Options;

namespace Console.TripperistaListExtractor.Parsing;

/// <summary>
/// Provides a contract for translating raw command line arguments into validated extraction options.
/// </summary>
public interface ICommandLineParser
{
    /// <summary>
    /// Parses the supplied <paramref name="args"/> into structured options.
    /// </summary>
    /// <param name="args">The command line arguments presented to the application entry point.</param>
    /// <returns>A parse result capturing success, errors, or help text requests.</returns>
    CommandLineParseResult Parse(string[] args);

    /// <summary>
    /// Creates the formatted usage text describing the available command line switches.
    /// </summary>
    /// <returns>A human-readable usage block.</returns>
    string GetUsage();
}
