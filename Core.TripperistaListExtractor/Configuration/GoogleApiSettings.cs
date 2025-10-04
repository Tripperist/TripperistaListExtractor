namespace Core.TripperistaListExtractor.Configuration;

using System.ComponentModel.DataAnnotations;

/// <summary>
///     Represents the strongly-typed configuration section used to supply Google Places credentials.
/// </summary>
public sealed class GoogleApiSettings
{
    /// <summary>
    ///     Gets or sets the API key that authorises Google Places requests.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "The Google Places API key cannot be empty.")]
    public string? ApiKey { get; set; }
}
