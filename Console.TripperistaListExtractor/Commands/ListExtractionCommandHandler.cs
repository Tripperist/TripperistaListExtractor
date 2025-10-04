using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using Core.TripperistaListExtractor.Commands;
using Core.TripperistaListExtractor.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Service.TripperistaListExtractor.Contracts;
using Console.TripperistaListExtractor.Resources;

namespace Console.TripperistaListExtractor.Commands;

/// <summary>
/// Handles the console command that orchestrates Google Maps list extraction.
/// </summary>
public sealed class ListExtractionCommandHandler(
    IGoogleMapsListExtractorService extractor,
    IConfiguration configuration,
    ILogger<ListExtractionCommandHandler> logger) : CommandHandler<ExtractionOptions>(logger)
{
    private readonly IGoogleMapsListExtractorService _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    /// <inheritdoc />
    protected override async Task ExecuteCoreAsync(ExtractionOptions options, CancellationToken cancellationToken)
    {
        ValidateOptions(options);

        var stopwatch = Stopwatch.StartNew();
        Logger.LogInformation(ResourceCatalog.Logs.GetString("CommandStarting") ?? "Starting extraction command.");

        var normalizedOptions = NormalizeOptions(options);
        await _extractor.ExtractAsync(normalizedOptions, cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        Logger.LogInformation(ResourceCatalog.Logs.GetString("CommandCompleted") ?? "Extraction command completed in {0} ms.", stopwatch.ElapsedMilliseconds);
    }

    private static void ValidateOptions(ExtractionOptions options)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(options);
        if (!Validator.TryValidateObject(options, context, results, true))
        {
            var message = ResourceCatalog.Errors.GetString("ValidationFailed") ?? "Validation failed";
            throw new ValidationException(string.Format(message, string.Join(", ", results.Select(r => r.ErrorMessage))));
        }
    }

    private ExtractionOptions NormalizeOptions(ExtractionOptions options)
    {
        var apiKey = options.GooglePlacesApiKey;
        string? source = null;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable("GOOGLE_PLACES_API_KEY");
            source = apiKey is null ? source : "environment";
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = _configuration["Google:PlacesApiKey"] ?? _configuration["googlePlacesApiKey"];
            source = apiKey is null ? source : "configuration";
        }

        if (!string.IsNullOrWhiteSpace(apiKey) && source is not null)
        {
            Logger.LogInformation(ResourceCatalog.Logs.GetString("UsingApiKeySource") ?? "Using Google Places API key from {0}.", source);
        }

        return new ExtractionOptions
        {
            InputSavedListUrl = options.InputSavedListUrl,
            OutputCsvFile = options.OutputCsvFile,
            OutputKmlFile = options.OutputKmlFile,
            GooglePlacesApiKey = apiKey,
            Headless = options.Headless,
            Verbose = options.Verbose,
        };
    }
}
