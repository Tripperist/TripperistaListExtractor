using Core.TripperistaListExtractor.Models;

namespace Service.TripperistaListExtractor.Contracts;

/// <summary>
/// Defines functionality for parsing Google Maps saved list payloads.
/// </summary>
public interface ISavedListPayloadParser
{
    /// <summary>
    /// Parses the supplied JavaScript payload into a strongly typed <see cref="SavedList"/>.
    /// </summary>
    /// <param name="payload">The raw JavaScript payload extracted from the page.</param>
    /// <returns>The parsed <see cref="SavedList"/>.</returns>
    SavedList Parse(string payload);
}
