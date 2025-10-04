namespace Service.TripperistaListExtractor.Writers;

using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Core.TripperistaListExtractor.Models;
using Microsoft.Extensions.Logging;
using Service.TripperistaListExtractor.Contracts;

/// <summary>
///     Implements CSV serialisation using the CsvHelper library.
/// </summary>
public sealed class CsvFileWriter(ILogger<CsvFileWriter> logger) : ICsvFileWriter
{
    private readonly ILogger<CsvFileWriter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task WriteAsync(SavedList list, string filePath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(filePath);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            LeaveOpen = false,
            NewLine = Environment.NewLine
        };

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
        await using var writer = new StreamWriter(stream, configuration.Encoding, bufferSize: 1024, leaveOpen: false);
        await using var csvWriter = new CsvWriter(writer, configuration);

        csvWriter.Context.RegisterClassMap<PlaceRecordMap>();

        await csvWriter.WriteHeaderAsync<PlaceRecord>(cancellationToken).ConfigureAwait(false);
        await csvWriter.NextRecordAsync().ConfigureAwait(false);

        foreach (var place in list.Places)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = new PlaceRecord(list.Header.Name, place.Name, place.Address, place.Latitude, place.Longitude, place.Note, place.ImageUrl);
            await csvWriter.WriteRecordAsync(record, cancellationToken).ConfigureAwait(false);
            await csvWriter.NextRecordAsync().ConfigureAwait(false);
        }

        await csvWriter.FlushAsync().ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);

        _logger.LogDebug("CSV output written to {FilePath}", filePath);
    }

    private sealed record PlaceRecord(
        string ListName,
        string PlaceName,
        string? Address,
        double? Latitude,
        double? Longitude,
        string? Note,
        string? ImageUrl);

    private sealed class PlaceRecordMap : ClassMap<PlaceRecord>
    {
        public PlaceRecordMap()
        {
            Map(record => record.ListName).Name("ListName");
            Map(record => record.PlaceName).Name("PlaceName");
            Map(record => record.Address).Name("Address");
            Map(record => record.Latitude).Name("Latitude");
            Map(record => record.Longitude).Name("Longitude");
            Map(record => record.Note).Name("Note");
            Map(record => record.ImageUrl).Name("ImageUrl");
        }
    }
}
