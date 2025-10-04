using System.Linq;
using System.Text.Json;
using Core.TripperistaListExtractor.Models;
using Microsoft.Extensions.Logging;
using Service.TripperistaListExtractor.Contracts;
using Service.TripperistaListExtractor.Resources;

namespace Service.TripperistaListExtractor.Parsers;

/// <summary>
/// Parses saved list payloads emitted by Google Maps scripts.
/// </summary>
public sealed class SavedListPayloadParser(ILogger<SavedListPayloadParser> logger) : ISavedListPayloadParser
{
    private readonly ILogger<SavedListPayloadParser> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public SavedList Parse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException(ResourceCatalog.Errors.GetString("InvalidPayload"), nameof(payload));
        }

        // Normalise the payload so it can be consumed as JSON regardless of whether it was string-escaped.
        var normalized = NormalizePayload(payload);

        using var document = JsonDocument.Parse(normalized, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException(ResourceCatalog.Errors.GetString("InvalidPayload"));
        }

        var rootArray = root.EnumerateArray().ToArray();
        var creator = ExtractCreator(rootArray);
        var (name, description) = ExtractHeader(rootArray);
        var places = ExtractPlaces(rootArray);

        var header = new SavedListHeader(
            name ?? "Untitled List",
            string.IsNullOrWhiteSpace(description) ? null : description,
            creator);

        // At debug level we record the parsed list name and item count to aid downstream troubleshooting.
        _logger.LogDebug(ResourceCatalog.Logs.GetString("ExtractionCompleted") ?? "Finished extraction for '{ListName}' with {PlaceCount} places.", header.Name, places.Count);

        return new SavedList(header, places);
    }

    private static string NormalizePayload(string payload)
    {
        const string sentinel = ")]}'\n";
        var trimmed = payload.Trim();
        if (trimmed.StartsWith(sentinel, StringComparison.Ordinal))
        {
            trimmed = trimmed[sentinel.Length..];
        }

        try
        {
            _ = JsonDocument.Parse(trimmed);
            return trimmed;
        }
        catch (JsonException)
        {
            // Attempt to unescape if the payload is still JSON encoded within a string literal.
            var unescaped = System.Text.RegularExpressions.Regex.Unescape(trimmed);
            return unescaped.Trim('"');
        }
    }

    private static string? ExtractCreator(JsonElement[] rootArray)
    {
        foreach (var element in rootArray)
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var values = element.EnumerateArray().ToArray();
            if (values.Length >= 3 &&
                values[0].ValueKind == JsonValueKind.String &&
                values[1].ValueKind == JsonValueKind.String &&
                values[2].ValueKind == JsonValueKind.String &&
                values[1].GetString()?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true)
            {
                return values[0].GetString();
            }
        }

        return null;
    }

    private static (string? Name, string? Description) ExtractHeader(JsonElement[] rootArray)
    {
        string? name = null;
        string? description = null;
        var ownerIndex = Array.FindIndex(rootArray, static e =>
            e.ValueKind == JsonValueKind.Array &&
            e.GetArrayLength() >= 2 &&
            e[0].ValueKind == JsonValueKind.String &&
            e[1].ValueKind == JsonValueKind.String &&
            e[1].GetString()?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true);

        var startIndex = ownerIndex >= 0 ? ownerIndex + 1 : 0;
        for (var index = startIndex; index < rootArray.Length; index++)
        {
            var element = rootArray[index];
            if (element.ValueKind == JsonValueKind.Array)
            {
                break;
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = element.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (name is null)
            {
                name = value.Trim();
                continue;
            }

            if (description is null)
            {
                description = value.Trim();
                break;
            }
        }

        return (name, description);
    }

    private IReadOnlyList<SavedPlace> ExtractPlaces(JsonElement[] rootArray)
    {
        foreach (var element in rootArray)
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var entry = element.EnumerateArray().FirstOrDefault(static e =>
                e.ValueKind == JsonValueKind.Array &&
                e.GetArrayLength() > 1 &&
                e[1].ValueKind == JsonValueKind.Array &&
                e[1].GetArrayLength() > 5 &&
                e[1][1].ValueKind == JsonValueKind.Array &&
                e[1][1].GetArrayLength() > 5 &&
                e[1][1][5].ValueKind == JsonValueKind.Array &&
                e[1][1][5].GetArrayLength() > 3);

            if (entry.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            return element.EnumerateArray()
                .Select(ExtractPlace)
                .Where(static place => place is not null)
                .Select(static place => place!)
                .ToArray();
        }

        throw new FormatException(ResourceCatalog.Errors.GetString("InvalidPayload"));
    }

    private SavedPlace? ExtractPlace(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() < 2)
        {
            return null;
        }

        var details = element[1];
        if (details.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var info = details.GetArrayLength() > 1 ? details[1] : default;
        var name = details.GetArrayLength() > 2 && details[2].ValueKind == JsonValueKind.String
            ? details[2].GetString() ?? string.Empty
            : string.Empty;
        var note = details.GetArrayLength() > 3 && details[3].ValueKind == JsonValueKind.String
            ? details[3].GetString()
            : null;

        string? address = null;
        double latitude = 0d;
        double longitude = 0d;

        if (info.ValueKind == JsonValueKind.Array && info.GetArrayLength() >= 6)
        {
            if (info[2].ValueKind == JsonValueKind.String)
            {
                address = info[2].GetString();
            }

            var location = info[5];
            if (location.ValueKind == JsonValueKind.Array && location.GetArrayLength() >= 4)
            {
                _ = double.TryParse(location[2].GetRawText(), out latitude);
                _ = double.TryParse(location[3].GetRawText(), out longitude);
            }
        }

        var imageUrl = ExtractImageUrl(details);

        return new SavedPlace(
            name,
            string.IsNullOrWhiteSpace(address) ? null : address,
            latitude,
            longitude,
            string.IsNullOrWhiteSpace(note) ? null : note,
            imageUrl);
    }

    private static string? ExtractImageUrl(JsonElement element)
    {
        foreach (var candidate in EnumerateStrings(element))
        {
            if (candidate.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                (candidate.Contains("googleusercontent", StringComparison.OrdinalIgnoreCase) || candidate.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateStrings(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                yield return element.GetString()!;
                break;
            case JsonValueKind.Array:
                foreach (var child in element.EnumerateArray())
                {
                    foreach (var value in EnumerateStrings(child))
                    {
                        yield return value;
                    }
                }
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    foreach (var value in EnumerateStrings(property.Value))
                    {
                        yield return value;
                    }
                }
                break;
        }
    }
}
