using Core.TripperistaListExtractor.Models;

namespace Core.TripperistaListExtractor.Services;

/// <summary>
/// Extracts saved lists from Google Maps.
/// </summary>
public interface ISavedListExtractionService
{
    /// <summary>
    /// Executes the extraction pipeline for the supplied list URL.
    /// </summary>
    /// <param name="listUrl">The Google Maps saved list URL.</param>
    /// <param name="cancellationToken">Propagates cancellation notifications.</param>
    /// <returns>The parsed <see cref="SavedList"/> result.</returns>
    Task<SavedList> ExtractAsync(Uri listUrl, CancellationToken cancellationToken);
}
