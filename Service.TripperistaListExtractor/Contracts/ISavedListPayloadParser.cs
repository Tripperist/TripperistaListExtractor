namespace Service.TripperistaListExtractor.Contracts;

using Core.TripperistaListExtractor.Models;
using System.Text.Json;

/// <summary>
///     Provides facilities for transforming raw payloads into strongly-typed saved list models.
/// </summary>
public interface ISavedListPayloadParser
{
    /// <summary>
    ///     Materialises a <see cref="SavedList"/> from the JSON payload extracted from the Google Maps page.
    /// </summary>
    /// <param name="payload">The parsed payload returned by <c>JSON.parse</c>.</param>
    /// <returns>The hydrated saved list instance.</returns>
    SavedList Parse(JsonElement payload);
}
