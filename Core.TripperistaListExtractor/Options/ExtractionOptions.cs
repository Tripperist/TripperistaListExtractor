using System.ComponentModel.DataAnnotations;
using Core.TripperistaListExtractor.Configuration;

namespace Core.TripperistaListExtractor.Options;

/// <summary>
/// Represents the validated command options for extracting a Google Maps saved list.
/// </summary>
public sealed class ExtractionOptions
{
    /// <summary>
    /// Gets or sets the Google Maps saved list URL to process.
    /// </summary>
    [Required]
    [Url]
    public required string InputSavedListUrl { get; init; }
        = string.Empty;

    /// <summary>
    /// Gets or sets the optional KML output file path.
    /// </summary>
    [NotEmptyOrWhitespace]
    public string? OutputKmlFile { get; init; }
        = default;

    /// <summary>
    /// Gets or sets the optional CSV output file path.
    /// </summary>
    [NotEmptyOrWhitespace]
    public string? OutputCsvFile { get; init; }
        = default;

    /// <summary>
    /// Gets or sets the Google Places API key used to enrich place metadata.
    /// </summary>
    [NotEmptyOrWhitespace]
    public string? GooglePlacesApiKey { get; init; }
        = default;

    /// <summary>
    /// Gets or sets a value indicating whether verbose logging is enabled.
    /// </summary>
    public bool Verbose { get; init; }
        = false;

    /// <summary>
    /// Gets or sets a value indicating whether the scraper should run headless.
    /// </summary>
    public bool Headless { get; init; }
        = true;
}
