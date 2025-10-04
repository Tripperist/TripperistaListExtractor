using System.Globalization;
using System.Text;
using System.Xml;
using Core.TripperistaListExtractor.Models;
using Microsoft.Extensions.Logging;
using Service.TripperistaListExtractor.Contracts;
using Service.TripperistaListExtractor.Resources;

namespace Service.TripperistaListExtractor.Writers;

/// <summary>
/// Implements KML persistence adhering to the Google KML specification.
/// </summary>
public sealed class KmlFileWriter(string path, ILogger<KmlFileWriter> logger) : IKmlFileWriter
{
    private readonly string _path = path ?? throw new ArgumentNullException(nameof(path));
    private readonly ILogger<KmlFileWriter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task WriteAsync(SavedList list, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);
        _logger.LogInformation(ResourceCatalog.Logs.GetString("KmlWriteStarted") ?? "Writing KML export to '{0}'.", _path);

        try
        {
            await using var stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            var settings = new XmlWriterSettings
            {
                Async = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = true,
                NewLineChars = Environment.NewLine,
            };

            await using var writer = XmlWriter.Create(stream, settings);
            await writer.WriteStartDocumentAsync().ConfigureAwait(false);
            await writer.WriteStartElementAsync(prefix: string.Empty, localName: "kml", ns: "http://www.opengis.net/kml/2.2").ConfigureAwait(false);
            await writer.WriteStartElementAsync(prefix: string.Empty, localName: "Document", ns: null).ConfigureAwait(false);

            await writer.WriteElementStringAsync(null, "name", null, list.Header.Name).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(list.Header.Description))
            {
                await writer.WriteElementStringAsync(null, "description", null, list.Header.Description).ConfigureAwait(false);
            }

            foreach (var place in list.Places)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteStartElementAsync(null, "Placemark", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(null, "name", null, place.Name).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(place.Note))
                {
                    await writer.WriteElementStringAsync(null, "description", null, place.Note).ConfigureAwait(false);
                }

                await writer.WriteStartElementAsync(null, "Point", null).ConfigureAwait(false);
                var coordinates = string.Create(CultureInfo.InvariantCulture, $"{place.Longitude},{place.Latitude},0");
                await writer.WriteElementStringAsync(null, "coordinates", null, coordinates).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false); // Point

                if (!string.IsNullOrWhiteSpace(place.Address))
                {
                    await writer.WriteElementStringAsync(null, "address", null, place.Address).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(place.ImageUrl))
                {
                    await writer.WriteStartElementAsync(null, "ExtendedData", null).ConfigureAwait(false);
                    await writer.WriteStartElementAsync(null, "Data", null).ConfigureAwait(false);
                    await writer.WriteAttributeStringAsync(null, "name", null, "imageUrl").ConfigureAwait(false);
                    await writer.WriteElementStringAsync(null, "value", null, place.ImageUrl).ConfigureAwait(false);
                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false); // Placemark
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false); // Document
            await writer.WriteEndElementAsync().ConfigureAwait(false); // kml
            await writer.WriteEndDocumentAsync().ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);

            _logger.LogInformation(ResourceCatalog.Logs.GetString("KmlWriteCompleted") ?? "KML export completed ({0} places).", list.Places.Count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, ResourceCatalog.Errors.GetString("KmlWriteFailed") ?? "Failed to persist KML output to path '{0}'.", _path);
            throw;
        }
    }
}
