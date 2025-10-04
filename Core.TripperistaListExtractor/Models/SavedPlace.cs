namespace Core.TripperistaListExtractor.Models;

/// <summary>
///     Represents a single place entry within a Google Maps saved list.
/// </summary>
public sealed class SavedPlace
{
    /// <summary>
    ///     Gets or sets the public facing place name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the full address string provided by Google Maps.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    ///     Gets or sets the latitude component of the place coordinates.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    ///     Gets or sets the longitude component of the place coordinates.
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    ///     Gets or sets the optional note supplied by the list author.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    ///     Gets or sets the optional image URL associated with the place entry.
    /// </summary>
    public string? ImageUrl { get; set; }
}
