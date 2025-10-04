namespace Core.TripperistaListExtractor.Options;

using System.ComponentModel.DataAnnotations;

/// <summary>
///     Represents the command-line options that control the behaviour of the list extraction workflow.
/// </summary>
public sealed class ExtractionOptions
{
    /// <summary>
    ///     Gets or sets the Google Maps saved list URL that should be scraped.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "The input saved list URL must be provided.")]
    [Url(ErrorMessage = "The input saved list URL must be a valid absolute URI.")]
    public string? InputSavedListUrl { get; set; }

    /// <summary>
    ///     Gets or sets the custom KML file name to generate. When omitted the list title will be used.
    /// </summary>
    [FileExtensions(Extensions = "kml", ErrorMessage = "The KML output must have a .kml extension.")]
    public string? OutputKmlFile { get; set; }

    /// <summary>
    ///     Gets or sets the custom CSV file name to generate. When omitted the list title will be used.
    /// </summary>
    [FileExtensions(Extensions = "csv", ErrorMessage = "The CSV output must have a .csv extension.")]
    public string? OutputCsvFile { get; set; }

    /// <summary>
    ///     Gets or sets the Google Places API key that augments the scraping results.
    /// </summary>
    public string? GooglePlacesApiKey { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether verbose console logging should be enabled.
    /// </summary>
    public bool Verbose { get; set; }
}
