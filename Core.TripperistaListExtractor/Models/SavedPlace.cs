namespace Core.TripperistaListExtractor.Models;

/// <summary>
/// Represents a single place entry inside a saved list.
/// </summary>
public sealed record SavedPlace(
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    string? Note,
    string? ImageUrl
);
