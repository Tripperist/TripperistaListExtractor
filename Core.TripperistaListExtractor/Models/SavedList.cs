namespace Core.TripperistaListExtractor.Models;

using System.Collections.ObjectModel;

/// <summary>
///     Represents the aggregate root that holds the saved list header and its places.
/// </summary>
public sealed class SavedList
{
    /// <summary>
    ///     Gets or sets the header metadata describing the list.
    /// </summary>
    public SavedListHeader Header { get; set; } = new();

    /// <summary>
    ///     Gets the places contained in the saved list.
    /// </summary>
    public Collection<SavedPlace> Places { get; } = new();
}
