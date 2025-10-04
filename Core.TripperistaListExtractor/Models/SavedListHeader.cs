namespace Core.TripperistaListExtractor.Models;

/// <summary>
/// Represents metadata describing a Google Maps saved list.
/// </summary>
/// <param name="Name">The user-visible list name.</param>
/// <param name="Description">The optional list description.</param>
/// <param name="Creator">The user that created the list.</param>
public sealed record class SavedListHeader(string Name, string? Description, string? Creator);
