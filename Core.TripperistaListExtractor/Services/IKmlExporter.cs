using Core.TripperistaListExtractor.Models;

namespace Core.TripperistaListExtractor.Services;

/// <summary>
/// Serializes saved lists into the KML format.
/// </summary>
public interface IKmlExporter
{
    /// <summary>
    /// Writes the saved list to the specified KML file.
    /// </summary>
    /// <param name="list">The saved list.</param>
    /// <param name="filePath">The destination file path.</param>
    /// <param name="cancellationToken">Propagates cancellation notifications.</param>
    Task WriteAsync(SavedList list, string filePath, CancellationToken cancellationToken);
}
