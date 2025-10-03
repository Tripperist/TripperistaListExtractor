using System.Linq;
using System.Text;
using System.Text.Json;
using Core.TripperistaListExtractor.Models;
using Microsoft.Extensions.Logging;

namespace Core.TripperistaListExtractor.Parsing;

/// <summary>
/// Parses the complex Google Maps payload into strongly typed models.
/// </summary>
public sealed class SavedListPayloadParser(ILogger<SavedListPayloadParser> logger) : ISavedListPayloadParser
{
    private readonly ILogger<SavedListPayloadParser> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public SavedList Parse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException(ResourceCatalog.GetErrorMessage("ScriptParsingFailed"), nameof(payload));
        }

        var normalized = Normalize(payload);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(normalized);
        }
        catch (JsonException jsonException)
        {
            var message = string.Format(ResourceCatalog.GetErrorMessage("ScriptParsingFailed"), jsonException.Message);
            _logger.LogError(jsonException, message);
            throw new InvalidOperationException(message, jsonException);
        }

        using (document)
        {
            var root = document.RootElement;
            var header = ExtractHeader(root);
            var places = ExtractPlaces(root);
            return new SavedList(header, places);
        }
    }

    private static SavedListHeader ExtractHeader(JsonElement root)
    {
        // The header information resides in the first collection of scalar values.
        // Google frequently tweaks the data shape, therefore we use a resilient search strategy here.
        string name = string.Empty;
        string description = string.Empty;
        string creator = string.Empty;
        string? creatorImage = null;

        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                var stringValues = element.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
                if (stringValues.Length >= 2 && string.IsNullOrEmpty(name))
                {
                    name = stringValues[0]!;
                    description = stringValues.Length > 1 ? stringValues[1]! : string.Empty;
                }
                else if (stringValues.Length >= 3 && string.IsNullOrEmpty(creator))
                {
                    creator = stringValues[0]!;
                    creatorImage = stringValues[1];
                }
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Google Maps Saved List";
        }

        return new SavedListHeader(name, description, creator, creatorImage);
    }

    private static IReadOnlyCollection<SavedPlace> ExtractPlaces(JsonElement root)
    {
        var results = new List<SavedPlace>();
        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var child in element.EnumerateArray())
            {
                if (child.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                var place = TryParsePlace(child);
                if (place is not null)
                {
                    results.Add(place);
                }
            }
        }

        return results;
    }

    private static SavedPlace? TryParsePlace(JsonElement node)
    {
        // The expected structure for a place entry contains a nested array with coordinates and textual fields.
        // We walk the nested arrays defensively to avoid throwing when the structure evolves.
        var name = string.Empty;
        var address = string.Empty;
        var note = default(string);
        var imageUrl = default(string);
        double latitude = 0;
        double longitude = 0;

        foreach (var child in node.EnumerateArray())
        {
            if (child.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(name))
            {
                name = child.GetString() ?? string.Empty;
            }
            else if (child.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(address))
            {
                address = child.GetString() ?? string.Empty;
            }
            else if (child.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(note))
            {
                note = child.GetString();
            }
            else if (child.ValueKind == JsonValueKind.Array)
            {
                foreach (var grandChild in child.EnumerateArray())
                {
                    if (grandChild.ValueKind == JsonValueKind.Array && grandChild.GetArrayLength() == 4)
                    {
                        var maybeLatitude = grandChild[2];
                        var maybeLongitude = grandChild[3];
                        if (maybeLatitude.ValueKind == JsonValueKind.Number && maybeLongitude.ValueKind == JsonValueKind.Number)
                        {
                            latitude = maybeLatitude.GetDouble();
                            longitude = maybeLongitude.GetDouble();
                        }
                    }
                    else if (grandChild.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(imageUrl) && grandChild.GetString()?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        imageUrl = grandChild.GetString();
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(name) || latitude.Equals(0) && longitude.Equals(0))
        {
            return null;
        }

        return new SavedPlace(name, address, latitude, longitude, note, imageUrl);
    }

    private static string Normalize(string payload)
    {
        var trimmed = payload.Trim();
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            return trimmed;
        }

        var markerIndex = trimmed.IndexOf("[", StringComparison.Ordinal);
        var lastMarkerIndex = trimmed.LastIndexOf("]", StringComparison.Ordinal);
        if (markerIndex >= 0 && lastMarkerIndex > markerIndex)
        {
            return trimmed.Substring(markerIndex, lastMarkerIndex - markerIndex + 1);
        }

        var builder = new StringBuilder(trimmed.Length);
        foreach (var character in trimmed)
        {
            if (!char.IsControl(character) || character == '\n' || character == '\r' || character == '\t')
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
