namespace Core.TripperistaListExtractor.Models;

/// <summary>
/// Represents a single place entry within a Google Maps saved list.
/// </summary>
/// <param name="Name">The place name.</param>
/// <param name="Address">The formatted address.</param>
/// <param name="Latitude">The geographic latitude.</param>
/// <param name="Longitude">The geographic longitude.</param>
/// <param name="Note">An optional user-specified note.</param>
/// <param name="ImageUrl">An optional preview image URL.</param>
public sealed record class SavedPlace(
    string Name,
    string? Address,
    double Latitude,
    double Longitude,
    string? Note,
    string? ImageUrl);
