using Microsoft.Extensions.Logging;
using Service.TripperistaListExtractor.Contracts;
using Service.TripperistaListExtractor.Writers;

namespace Service.TripperistaListExtractor.Implementations;

/// <summary>
/// Provides factory methods for CSV and KML writers.
/// </summary>
public sealed class FileWriterFactory(ILoggerFactory loggerFactory) : IFileWriterFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    /// <inheritdoc />
    public ICsvFileWriter CreateCsv(string path)
        => new CsvFileWriter(path, _loggerFactory.CreateLogger<CsvFileWriter>());

    /// <inheritdoc />
    public IKmlFileWriter CreateKml(string path)
        => new KmlFileWriter(path, _loggerFactory.CreateLogger<KmlFileWriter>());
}
