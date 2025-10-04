using System.Globalization;
using System.Text;
using Core.TripperistaListExtractor.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Service.TripperistaListExtractor.Contracts;
using Service.TripperistaListExtractor.Resources;

namespace Service.TripperistaListExtractor.Writers;

/// <summary>
/// Implements CSV persistence using CsvHelper.
/// </summary>
public sealed class CsvFileWriter(string path, ILogger<CsvFileWriter> logger) : ICsvFileWriter
{
    private readonly string _path = path ?? throw new ArgumentNullException(nameof(path));
    private readonly ILogger<CsvFileWriter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task WriteAsync(SavedList list, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);

        _logger.LogInformation(ResourceCatalog.Logs.GetString("CsvWriteStarted") ?? "Writing CSV export to '{0}'.", _path);

        try
        {
            await using var stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
            await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                NewLine = Environment.NewLine,
            });

            csv.WriteHeader<CsvRecord>();
            await csv.NextRecordAsync().ConfigureAwait(false);

            foreach (var place in list.Places)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var record = new CsvRecord(place.Name, place.Address, place.Latitude, place.Longitude, place.Note, place.ImageUrl);
                csv.WriteRecord(record);
                await csv.NextRecordAsync().ConfigureAwait(false);
            }

            await writer.FlushAsync().ConfigureAwait(false);
            _logger.LogInformation(ResourceCatalog.Logs.GetString("CsvWriteCompleted") ?? "CSV export completed ({0} places).", list.Places.Count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, ResourceCatalog.Errors.GetString("CsvWriteFailed") ?? "Failed to persist CSV output to path '{0}'.", _path);
            throw;
        }
    }

    private sealed record CsvRecord(
        string Name,
        string? Address,
        double Latitude,
        double Longitude,
        string? Note,
        string? ImageUrl);
}
