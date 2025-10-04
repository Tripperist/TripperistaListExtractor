using Core.TripperistaListExtractor.Models;
using Core.TripperistaListExtractor.Options;

namespace Service.TripperistaListExtractor.Contracts;

/// <summary>
/// Defines the behavior required to extract saved list data from Google Maps.
/// </summary>
public interface IGoogleMapsListExtractorService
{
    /// <summary>
    /// Extracts the saved list data using the supplied <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The validated extraction options.</param>
    /// <param name="cancellationToken">The cancellation token for cooperative cancellation.</param>
    /// <returns>The hydrated <see cref="SavedList"/>.</returns>
    Task<SavedList> ExtractAsync(ExtractionOptions options, CancellationToken cancellationToken);
}
