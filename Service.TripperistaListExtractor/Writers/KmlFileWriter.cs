namespace Service.TripperistaListExtractor.Writers;

using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Core.TripperistaListExtractor.Models;
using Microsoft.Extensions.Logging;
using Service.TripperistaListExtractor.Contracts;

/// <summary>
///     Serialises the saved list into the Keyhole Markup Language format.
/// </summary>
public sealed class KmlFileWriter(ILogger<KmlFileWriter> logger) : IKmlFileWriter
{
    private readonly ILogger<KmlFileWriter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

        var kmlNamespace = XNamespace.Get("http://www.opengis.net/kml/2.2");
        var documentElement = new XElement(kmlNamespace + "Document",
            new XElement(kmlNamespace + "name", list.Header.Name),
            new XElement(kmlNamespace + "description", list.Header.Description ?? string.Empty));

        foreach (var place in list.Places)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var placemark = new XElement(kmlNamespace + "Placemark",
                new XElement(kmlNamespace + "name", place.Name),
                new XElement(kmlNamespace + "description", BuildDescriptionFragment(place)),
                new XElement(kmlNamespace + "Point",
                    new XElement(kmlNamespace + "coordinates", FormatCoordinates(place.Longitude, place.Latitude))));

            documentElement.Add(placemark);
        }

        var kmlDocument = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(kmlNamespace + "kml", documentElement));

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
        await Task.Run(() => kmlDocument.Save(stream), cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("KML output written to {FilePath}", filePath);
    }

    private static string FormatCoordinates(double? longitude, double? latitude)
    {
        if (longitude is null || latitude is null)
        {
            return string.Empty;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{longitude.Value},{latitude.Value},0");
    }

    private static string BuildDescriptionFragment(SavedPlace place)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(place.Address))
        {
            builder.AppendLine(place.Address);
        }

        if (!string.IsNullOrWhiteSpace(place.Note))
        {
            builder.AppendLine(place.Note);
        }

        if (!string.IsNullOrWhiteSpace(place.ImageUrl))
        {
            builder.Append("Image: ").Append(place.ImageUrl);
        }

        return builder.ToString().Trim();
    }
}
