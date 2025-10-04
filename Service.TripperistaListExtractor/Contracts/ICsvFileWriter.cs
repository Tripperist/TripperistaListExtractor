namespace Service.TripperistaListExtractor.Contracts;

using Core.TripperistaListExtractor.Models;

/// <summary>
///     Defines a component that can persist saved list data to a CSV file.
/// </summary>
public interface ICsvFileWriter
{
    /// <summary>
    ///     Writes the supplied saved list to a CSV document.
    /// </summary>
    /// <param name="list">The saved list to serialise.</param>
    /// <param name="filePath">The destination file path.</param>
    /// <param name="cancellationToken">The cancellation token that coordinates shutdown.</param>
    /// <returns>A task that completes when the file has been written.</returns>
    Task WriteAsync(SavedList list, string filePath, CancellationToken cancellationToken);
}
