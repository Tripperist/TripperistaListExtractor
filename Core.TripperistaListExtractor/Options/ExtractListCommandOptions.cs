using System.ComponentModel.DataAnnotations;

namespace Core.TripperistaListExtractor.Options;

/// <summary>
/// Encapsulates the command line arguments necessary to execute the extraction workflow.
/// </summary>
public sealed class ExtractListCommandOptions
{
    /// <summary>
    /// Gets or sets the saved list URL supplied on the command line.
    /// </summary>
    [Required(ErrorMessage = "The --inputSavedListUrl argument is mandatory.")]
    public Uri? InputSavedListUrl { get; set; }

    /// <summary>
    /// Gets or sets the optional KML file path override.
    /// </summary>
    [RegularExpression(@"^.*\\.kml$", ErrorMessage = "KML file names must use the .kml extension.")]
    public string? OutputKmlFile { get; set; }

    /// <summary>
    /// Gets or sets the optional CSV file path override.
    /// </summary>
    [RegularExpression(@"^.*\\.csv$", ErrorMessage = "CSV file names must use the .csv extension.")]
    public string? OutputCsvFile { get; set; }

    /// <summary>
    /// Gets or sets the Google Places API key when available from the command line.
    /// </summary>
    public string? GooglePlacesApiKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether verbose console logging is enabled.
    /// </summary>
    public bool Verbose { get; set; }
}
