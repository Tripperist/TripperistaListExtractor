using Core.TripperistaListExtractor.Models;

namespace Core.TripperistaListExtractor.Services;

/// <summary>
/// Serializes saved list data into CSV format.
/// </summary>
public interface ICsvExporter
{
    /// <summary>
    /// Persists the saved list to CSV using the supplied file path.
    /// </summary>
    /// <param name="list">The saved list to serialize.</param>
    /// <param name="filePath">The destination file path.</param>
    /// <param name="cancellationToken">Propagates cancellation notifications.</param>
    Task WriteAsync(SavedList list, string filePath, CancellationToken cancellationToken);
}
