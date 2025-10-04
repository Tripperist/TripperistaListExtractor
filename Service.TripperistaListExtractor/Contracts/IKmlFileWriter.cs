using Core.TripperistaListExtractor.Models;

namespace Service.TripperistaListExtractor.Contracts;

/// <summary>
/// Writes saved list data to a KML file.
/// </summary>
public interface IKmlFileWriter
{
    /// <summary>
    /// Persists the <paramref name="list"/> to KML format.
    /// </summary>
    /// <param name="list">The saved list to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteAsync(SavedList list, CancellationToken cancellationToken);
}
