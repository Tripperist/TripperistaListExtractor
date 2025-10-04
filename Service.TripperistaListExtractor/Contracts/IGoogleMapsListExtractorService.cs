namespace Service.TripperistaListExtractor.Contracts;

using Core.TripperistaListExtractor.Models;

/// <summary>
///     Defines the behaviour required to extract saved list information from Google Maps.
/// </summary>
public interface IGoogleMapsListExtractorService
{
    /// <summary>
    ///     Extracts the saved list information located at the specified URI.
    /// </summary>
    /// <param name="listUri">The URI of the Google Maps saved list.</param>
    /// <param name="verbose">A value indicating whether verbose diagnostics should be emitted.</param>
    /// <param name="cancellationToken">The cancellation token that coordinates shutdown.</param>
    /// <returns>The fully populated saved list.</returns>
    Task<SavedList> ExtractAsync(Uri listUri, bool verbose, CancellationToken cancellationToken);
}
