using Core.TripperistaListExtractor.Models;

namespace Service.TripperistaListExtractor.Contracts;

/// <summary>
/// Provides file writer instances for persisting saved list data.
/// </summary>
public interface IFileWriterFactory
{
    /// <summary>
    /// Creates a CSV file writer targeting the supplied <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file system path.</param>
    /// <returns>An <see cref="ICsvFileWriter"/>.</returns>
    ICsvFileWriter CreateCsv(string path);

    /// <summary>
    /// Creates a KML file writer targeting the supplied <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file system path.</param>
    /// <returns>An <see cref="IKmlFileWriter"/>.</returns>
    IKmlFileWriter CreateKml(string path);
}
