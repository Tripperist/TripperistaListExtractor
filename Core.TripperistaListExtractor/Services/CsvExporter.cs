using System.IO;
using Core.TripperistaListExtractor.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.TripperistaListExtractor.Services;

/// <summary>
/// CSV serialization implemented with CsvHelper.
/// </summary>
public sealed class CsvExporter(ILogger<CsvExporter> logger) : ICsvExporter
{
    private readonly ILogger<CsvExporter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task WriteAsync(SavedList list, string filePath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            await using var streamWriter = new StreamWriter(filePath);
            await using var csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            });

            await csvWriter.WriteRecordsAsync(list.Places, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var message = string.Format(ResourceCatalog.GetErrorMessage("FileWriteFailed"), filePath, exception.Message);
            _logger.LogError(exception, message);
            throw new InvalidOperationException(message, exception);
        }
    }
}
