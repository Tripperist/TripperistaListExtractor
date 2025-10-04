using System.ComponentModel.DataAnnotations;
using Core.TripperistaListExtractor.Configuration;

namespace Core.TripperistaListExtractor.Configuration;

/// <summary>
/// Represents configuration required to access Google APIs.
/// </summary>
public sealed class GoogleApiSettings
{
    /// <summary>
    /// Gets or sets the Google Places API key.
    /// </summary>
    [Required]
    [NotEmptyOrWhitespace]
    public string ApiKey { get; init; } = string.Empty;
}
