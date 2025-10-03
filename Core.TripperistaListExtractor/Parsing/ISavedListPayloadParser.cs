using Core.TripperistaListExtractor.Models;

namespace Core.TripperistaListExtractor.Parsing;

/// <summary>
/// Defines an abstraction capable of parsing the serialized Google Maps payload into strongly-typed structures.
/// </summary>
public interface ISavedListPayloadParser
{
    /// <summary>
    /// Parses the provided payload and constructs a <see cref="SavedList"/> instance.
    /// </summary>
    /// <param name="payload">The serialized payload extracted from the Google Maps script block.</param>
    /// <returns>A populated <see cref="SavedList"/> structure.</returns>
    SavedList Parse(string payload);
}
