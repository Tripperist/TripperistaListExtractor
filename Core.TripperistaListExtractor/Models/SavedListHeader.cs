namespace Core.TripperistaListExtractor.Models;

/// <summary>
/// Represents the high-level metadata describing a Google Maps saved list.
/// </summary>
public sealed record SavedListHeader(
    string Name,
    string Description,
    string CreatorName,
    string? CreatorProfileImageUrl
);
