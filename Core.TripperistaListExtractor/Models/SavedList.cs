namespace Core.TripperistaListExtractor.Models;

/// <summary>
/// Represents a saved list including metadata and the contained places.
/// </summary>
public sealed record SavedList(
    SavedListHeader Header,
    IReadOnlyCollection<SavedPlace> Places
);
