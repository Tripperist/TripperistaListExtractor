using Console.TripperistaListExtractor.Resources;
using Core.TripperistaListExtractor.Options;
using Microsoft.Extensions.Logging;

namespace Console.TripperistaListExtractor.Parsing;

/// <summary>
/// Parses command line arguments without relying on external command line frameworks so the
/// application remains resilient to package feed drift while still supporting rich diagnostics.
/// </summary>
public sealed class CommandLineParser(ILogger<CommandLineParser> logger) : ICommandLineParser
{
    private readonly ILogger<CommandLineParser> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public CommandLineParseResult Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
        {
            // An empty invocation is treated as a documentation request so the user immediately sees
            // the supported switches without needing to guess at the --help flag.
            _logger.LogDebug("No arguments supplied; defaulting to help output.");
            return new CommandLineParseResult
            {
                ShowHelp = true,
            };
        }

        string? inputUrl = null;
        string? csvPath = null;
        string? kmlPath = null;
        string? apiKey = null;
        var verbose = false;
        var headless = true;

        for (var index = 0; index < args.Length; index++)
        {
            var token = args[index];
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (IsOption(token, out var normalized))
            {
                if (IsHelpSwitch(normalized))
                {
                    // Short-circuit for help to avoid validating the remainder of the command line.
                    _logger.LogDebug("Help switch detected.");
                    return new CommandLineParseResult
                    {
                        ShowHelp = true,
                    };
                }

                switch (normalized)
                {
                    case "--inputsavedlisturl":
                        {
                            // Required argument that points to the Google Maps saved list.
                            if (!TryReadValue(args, ref index, out inputUrl, out var error))
                            {
                                return CreateError(error ?? FormatError("MissingOptionValue", token));
                            }

                            break;
                        }

                    case "--outputcsvfile":
                        {
                            // Optional CSV path overrides the sanitized list name fallback.
                            if (!TryReadValue(args, ref index, out csvPath, out var error))
                            {
                                return CreateError(error ?? FormatError("MissingOptionValue", token));
                            }

                            break;
                        }

                    case "--outputkmlfile":
                        {
                            // Optional KML path overrides the sanitized list name fallback.
                            if (!TryReadValue(args, ref index, out kmlPath, out var error))
                            {
                                return CreateError(error ?? FormatError("MissingOptionValue", token));
                            }

                            break;
                        }

                    case "--googleplacesapikey":
                        {
                            // Pull through an API key override so downstream enrichment can occur.
                            if (!TryReadValue(args, ref index, out apiKey, out var error))
                            {
                                return CreateError(error ?? FormatError("MissingOptionValue", token));
                            }

                            break;
                        }

                    case "--headless":
                        {
                            // When no explicit value is present we accept the default "true".
                            if (PeekIsOption(args, index + 1))
                            {
                                headless = true;
                                break;
                            }

                            if (!TryReadValue(args, ref index, out var value, out var error))
                            {
                                return CreateError(error ?? FormatError("MissingOptionValue", token));
                            }

                            // Support explicit true/false toggles so automation scenarios can disable headless mode.
                            if (!bool.TryParse(value, out headless))
                            {
                                return CreateError(FormatError("InvalidBoolean", token, value));
                            }

                            break;
                        }

                    case "--no-headless":
                        {
                            // Alias mirroring browsers' conventional "no-" toggles for clarity.
                            headless = false;
                            break;
                        }

                    case "--verbose":
                    case "-v":
                        {
                            // Treat presence of the switch with no argument as "true" for convenience.
                            if (PeekIsOption(args, index + 1))
                            {
                                verbose = true;
                                break;
                            }

                            if (!TryReadValue(args, ref index, out var value, out var error))
                            {
                                return CreateError(error ?? FormatError("MissingOptionValue", token));
                            }

                            // Allow "true"/"false" toggles so automation can explicitly disable verbose logging.
                            if (!bool.TryParse(value, out verbose))
                            {
                                return CreateError(FormatError("InvalidBoolean", token, value));
                            }

                            break;
                        }

                    default:
                        {
                            return CreateError(FormatError("UnknownOption", token));
                        }
                }

                continue;
            }

            return CreateError(FormatError("UnknownOption", token));
        }

        if (string.IsNullOrWhiteSpace(inputUrl))
        {
            // The saved list URL is the only required option; without it we fail fast before contacting Playwright.
            return CreateError(ResourceCatalog.Errors.GetString("MissingInputUrl"));
        }

        var options = new ExtractionOptions
        {
            InputSavedListUrl = inputUrl!,
            OutputCsvFile = string.IsNullOrWhiteSpace(csvPath) ? null : csvPath,
            OutputKmlFile = string.IsNullOrWhiteSpace(kmlPath) ? null : kmlPath,
            GooglePlacesApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
            Verbose = verbose,
            Headless = headless,
        };

        _logger.LogDebug("Successfully parsed command line arguments.");
        return new CommandLineParseResult
        {
            Options = options,
        };
    }

    /// <inheritdoc />
    public string GetUsage()
        => ResourceCatalog.Help.GetString("Usage") ?? string.Empty;

    private static bool IsOption(string token, out string normalized)
    {
        normalized = token.Trim();
        if (!normalized.StartsWith("--", StringComparison.Ordinal) && !normalized.StartsWith("-", StringComparison.Ordinal))
        {
            return false;
        }

        normalized = normalized.ToLowerInvariant();
        return true;
    }

    private static bool IsHelpSwitch(string option)
        => string.Equals(option, "--help", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(option, "-h", StringComparison.OrdinalIgnoreCase);

    private static bool TryReadValue(string[] args, ref int index, out string? value, out string? error)
    {
        var nextIndex = index + 1;
        if (nextIndex >= args.Length || PeekIsOption(args, nextIndex))
        {
            value = null;
            error = null;
            return false;
        }

        value = args[nextIndex];
        error = null;
        index = nextIndex;
        return true;
    }

    private static bool PeekIsOption(string[] args, int index)
        => index < args.Length && args[index].StartsWith("-", StringComparison.Ordinal);

    private static CommandLineParseResult CreateError(string? message)
        => new()
        {
            ErrorMessage = message,
        };

    private static string FormatError(string resourceKey, params object[] parameters)
    {
        var template = ResourceCatalog.Errors.GetString(resourceKey);
        return string.IsNullOrEmpty(template)
            ? string.Empty
            : string.Format(template, parameters);
    }
}
