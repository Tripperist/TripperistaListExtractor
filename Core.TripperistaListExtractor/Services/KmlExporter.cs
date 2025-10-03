using System.IO;
using System.Xml;
using Core.TripperistaListExtractor.Models;
using Microsoft.Extensions.Logging;

namespace Core.TripperistaListExtractor.Services;

/// <summary>
/// Generates KML documents following the Keyhole Markup Language specification.
/// </summary>
public sealed class KmlExporter(ILogger<KmlExporter> logger) : IKmlExporter
{
    private readonly ILogger<KmlExporter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task WriteAsync(SavedList list, string filePath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var settings = new XmlWriterSettings
            {
                Async = true,
                Indent = true,
                Encoding = System.Text.Encoding.UTF8
            };

            await using var writer = XmlWriter.Create(stream, settings);
            await writer.WriteStartDocumentAsync().ConfigureAwait(false);
            await writer.WriteStartElementAsync(prefix: null, localName: "kml", ns: "http://www.opengis.net/kml/2.2").ConfigureAwait(false);
            await writer.WriteStartElementAsync(null, "Document", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(null, "name", null, list.Header.Name).ConfigureAwait(false);
            await writer.WriteElementStringAsync(null, "description", null, list.Header.Description).ConfigureAwait(false);

            foreach (var place in list.Places)
            {
                await writer.WriteStartElementAsync(null, "Placemark", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(null, "name", null, place.Name).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(place.Note))
                {
                    await writer.WriteElementStringAsync(null, "description", null, place.Note).ConfigureAwait(false);
                }

                await writer.WriteStartElementAsync(null, "Point", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(null, "coordinates", null, $"{place.Longitude},{place.Latitude},0").ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndDocumentAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var message = string.Format(ResourceCatalog.GetErrorMessage("FileWriteFailed"), filePath, exception.Message);
            _logger.LogError(exception, message);
            throw new InvalidOperationException(message, exception);
        }
    }
}
