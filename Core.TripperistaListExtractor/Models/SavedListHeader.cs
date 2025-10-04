namespace Core.TripperistaListExtractor.Models;

/// <summary>
///     Represents the metadata that accompanies a Google Maps saved list.
/// </summary>
public sealed class SavedListHeader
{
    /// <summary>
    ///     Gets or sets the title of the Google Maps list.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the description authored for the list.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the name of the list creator.
    /// </summary>
    public string Creator { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the source URL for the creator's avatar when available.
    /// </summary>
    public string? CreatorImageUrl { get; set; }
}
