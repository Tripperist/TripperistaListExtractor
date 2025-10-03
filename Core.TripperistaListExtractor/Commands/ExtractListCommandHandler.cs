using Core.TripperistaListExtractor.Options;
using Core.TripperistaListExtractor.Services;
using Microsoft.Extensions.Logging;

namespace Core.TripperistaListExtractor.Commands;

/// <summary>
/// Concrete command handler orchestrating extraction and file generation.
/// </summary>
public sealed class ExtractListCommandHandler(
    ILogger<ExtractListCommandHandler> logger,
    ISavedListExtractionService extractionService,
    ICsvExporter csvExporter,
    IKmlExporter kmlExporter
) : CommandHandler<ExtractListCommandOptions>(logger)
{
    private readonly ILogger<ExtractListCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ISavedListExtractionService _extractionService = extractionService ?? throw new ArgumentNullException(nameof(extractionService));
    private readonly ICsvExporter _csvExporter = csvExporter ?? throw new ArgumentNullException(nameof(csvExporter));
    private readonly IKmlExporter _kmlExporter = kmlExporter ?? throw new ArgumentNullException(nameof(kmlExporter));

    /// <inheritdoc />
    protected override async Task<int> ExecuteCoreAsync(ExtractListCommandOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options.InputSavedListUrl);
        var savedList = await _extractionService.ExtractAsync(options.InputSavedListUrl, cancellationToken).ConfigureAwait(false);
        var baseFileName = savedList.Header.Name.Replace(' ', '_');

        if (!string.IsNullOrWhiteSpace(options.OutputCsvFile))
        {
            await _csvExporter.WriteAsync(savedList, options.OutputCsvFile, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(ResourceCatalog.GetLogMessage("WritingCsv"), options.OutputCsvFile);
        }
        else
        {
            var csvPath = $"{baseFileName}.csv";
            await _csvExporter.WriteAsync(savedList, csvPath, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(ResourceCatalog.GetLogMessage("WritingCsv"), csvPath);
        }

        if (!string.IsNullOrWhiteSpace(options.OutputKmlFile))
        {
            await _kmlExporter.WriteAsync(savedList, options.OutputKmlFile, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(ResourceCatalog.GetLogMessage("WritingKml"), options.OutputKmlFile);
        }
        else
        {
            var kmlPath = $"{baseFileName}.kml";
            await _kmlExporter.WriteAsync(savedList, kmlPath, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(ResourceCatalog.GetLogMessage("WritingKml"), kmlPath);
        }

        _logger.LogInformation(ResourceCatalog.GetLogMessage("CompletedExtraction"));
        return 0;
    }
}
