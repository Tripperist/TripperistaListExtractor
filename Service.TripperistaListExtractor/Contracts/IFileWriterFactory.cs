namespace Service.TripperistaListExtractor.Contracts;

/// <summary>
///     Implements the factory pattern to build file writer services on demand.
/// </summary>
public interface IFileWriterFactory
{
    /// <summary>
    ///     Resolves a CSV writer implementation.
    /// </summary>
    /// <returns>The CSV writer service.</returns>
    ICsvFileWriter CreateCsvWriter();

    /// <summary>
    ///     Resolves a KML writer implementation.
    /// </summary>
    /// <returns>The KML writer service.</returns>
    IKmlFileWriter CreateKmlWriter();
}
