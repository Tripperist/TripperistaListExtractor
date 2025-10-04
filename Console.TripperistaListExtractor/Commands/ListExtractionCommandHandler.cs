namespace Console.TripperistaListExtractor.Commands;

using System.IO;
using System.Text;
using Console.TripperistaListExtractor.Hosting;
using Core.TripperistaListExtractor.Commands;
using Core.TripperistaListExtractor.Models;
using Core.TripperistaListExtractor.Options;
using Microsoft.Extensions.Logging;
using Service.TripperistaListExtractor.Contracts;

/// <summary>
///     Implements the concrete command used to orchestrate the end-to-end extraction workflow.
/// </summary>
public sealed class ListExtractionCommandHandler(
    ResourceBundle resourceBundle,
    ILogger<ListExtractionCommandHandler> logger,
    IGoogleMapsListExtractorService extractorService,
    IFileWriterFactory fileWriterFactory
) : CommandHandler<ExtractionOptions>(resourceBundle.LogResourceManager, resourceBundle.ErrorResourceManager)
{
    private readonly ILogger<ListExtractionCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IGoogleMapsListExtractorService _extractorService = extractorService ?? throw new ArgumentNullException(nameof(extractorService));
    private readonly IFileWriterFactory _fileWriterFactory = fileWriterFactory ?? throw new ArgumentNullException(nameof(fileWriterFactory));

    /// <inheritdoc />
    protected override async Task<int> ExecuteInternalAsync(ExtractionOptions options, CancellationToken cancellationToken)
    {
        var inputUri = new Uri(options.InputSavedListUrl!, UriKind.Absolute);
        using var scope = _logger.BeginScope("InputUrl:{InputUrl}", inputUri);

        _logger.LogInformation(GetLogMessage("ExtractionStarting"), inputUri);

        SavedList savedList;
        try
        {
            savedList = await _extractorService.ExtractAsync(inputUri, options.Verbose, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, GetErrorMessage("ExtractionFailed"), inputUri);
            return -1;
        }

        var baseFileName = CreateSafeFileStem(savedList.Header.Name);
        var kmlPath = ResolveOutputPath(options.OutputKmlFile, baseFileName, ".kml");
        var csvPath = ResolveOutputPath(options.OutputCsvFile, baseFileName, ".csv");

        try
        {
            if (!string.IsNullOrWhiteSpace(kmlPath))
            {
                _logger.LogInformation(GetLogMessage("WritingKmlMessage"), kmlPath);
                var kmlWriter = _fileWriterFactory.CreateKmlWriter();
                await kmlWriter.WriteAsync(savedList, kmlPath!, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(csvPath))
            {
                _logger.LogInformation(GetLogMessage("WritingCsvMessage"), csvPath);
                var csvWriter = _fileWriterFactory.CreateCsvWriter();
                await csvWriter.WriteAsync(savedList, csvPath!, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, GetErrorMessage("FileWriteFailed"));
            return -2;
        }

        _logger.LogInformation(GetLogMessage("ExtractionCompleted"), savedList.Places.Count);
        return 0;
    }

    /// <summary>
    ///     Builds a deterministic file path using the supplied file name or a safe representation of the list title.
    /// </summary>
    /// <param name="providedPath">The file name supplied on the command line.</param>
    /// <param name="fallbackStem">The fallback file stem derived from the list name.</param>
    /// <param name="extension">The expected file extension including the leading dot.</param>
    /// <returns>The fully qualified file path.</returns>
    private static string ResolveOutputPath(string? providedPath, string fallbackStem, string extension)
    {
        var candidate = providedPath;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = fallbackStem + extension;
        }
        else if (!candidate.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate + extension;
        }

        var directory = Path.GetDirectoryName(candidate);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return Path.GetFullPath(candidate!);
    }

    /// <summary>
    ///     Sanitises the supplied stem to ensure it can be used as a file name on all platforms.
    /// </summary>
    /// <param name="name">The original name extracted from the saved list.</param>
    /// <returns>A safe file stem.</returns>
    private static string CreateSafeFileStem(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "tripperista-list";
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            builder.Append(Array.IndexOf(invalidCharacters, ch) >= 0 ? '_' : ch);
        }

        return builder.ToString().Trim();
    }
}
