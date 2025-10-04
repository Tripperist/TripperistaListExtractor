namespace Core.TripperistaListExtractor.Models;

/// <summary>
/// Represents a hydrated Google Maps saved list including metadata and places.
/// </summary>
/// <param name="Header">The list header metadata.</param>
/// <param name="Places">The collection of saved places.</param>
public sealed record class SavedList(SavedListHeader Header, IReadOnlyList<SavedPlace> Places);
