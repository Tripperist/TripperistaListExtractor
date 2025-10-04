namespace Service.TripperistaListExtractor.Contracts;

using Core.TripperistaListExtractor.Models;

/// <summary>
///     Exposes the behaviour required to persist saved list data to a KML file.
/// </summary>
public interface IKmlFileWriter
{
    /// <summary>
    ///     Writes the supplied saved list to the specified KML file path.
    /// </summary>
    /// <param name="list">The saved list to serialise.</param>
    /// <param name="filePath">The destination file path.</param>
    /// <param name="cancellationToken">The cancellation token that coordinates shutdown.</param>
    /// <returns>A task that completes when the file has been written.</returns>
    Task WriteAsync(SavedList list, string filePath, CancellationToken cancellationToken);
}
