namespace Service.TripperistaListExtractor.Parsers;

using System.Linq;
using System.Text.Json;
using Core.TripperistaListExtractor.Models;
using Service.TripperistaListExtractor.Contracts;

/// <summary>
///     Converts the loosely structured Google Maps payload into strongly typed domain entities.
/// </summary>
public sealed class SavedListPayloadParser : ISavedListPayloadParser
{
    /// <inheritdoc />
    public SavedList Parse(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("The payload root must be a JSON array.");
        }

        var list = new SavedList
        {
            Header = ExtractHeader(payload)
        };

        foreach (var placeNode in FindPlaceNodes(payload))
        {
            var place = MapPlace(placeNode);
            if (!string.IsNullOrWhiteSpace(place.Name))
            {
                list.Places.Add(place);
            }
        }

        return list;
    }

    private static SavedListHeader ExtractHeader(JsonElement root)
    {
        string? listName = null;
        string? listDescription = null;
        string? creatorName = null;
        string? creatorImage = null;

        foreach (var element in Traverse(root))
        {
            if (creatorName is null && TryReadCreator(element, out var potentialCreator, out var potentialImage))
            {
                creatorName = potentialCreator;
                creatorImage = potentialImage;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                var value = element.GetString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (listName is null)
                {
                    listName = value;
                    continue;
                }

                if (listDescription is null && !value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    listDescription = value;
                }
            }
        }

        return new SavedListHeader
        {
            Name = listName ?? string.Empty,
            Description = listDescription,
            Creator = creatorName ?? string.Empty,
            CreatorImageUrl = creatorImage
        };
    }

    private static bool TryReadCreator(JsonElement element, out string? creatorName, out string? creatorImage)
    {
        creatorName = null;
        creatorImage = null;

        if (element.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var values = element.EnumerateArray().ToArray();
        if (values.Length < 3)
        {
            return false;
        }

        if (values[0].ValueKind == JsonValueKind.String &&
            values[1].ValueKind == JsonValueKind.String &&
            values[2].ValueKind == JsonValueKind.String &&
            values[1].GetString()?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true)
        {
            creatorName = values[0].GetString();
            creatorImage = values[1].GetString();
            return true;
        }

        return false;
    }

    private static IEnumerable<JsonElement> Traverse(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    yield return item;
                    foreach (var nested in Traverse(item))
                    {
                        yield return nested;
                    }
                }

                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    yield return property.Value;
                    foreach (var nested in Traverse(property.Value))
                    {
                        yield return nested;
                    }
                }

                break;
        }
    }

    private static IEnumerable<JsonElement> FindPlaceNodes(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            var values = root.EnumerateArray().ToArray();
            if (IsPlaceNode(values))
            {
                yield return root;
            }

            foreach (var child in values)
            {
                foreach (var nested in FindPlaceNodes(child))
                {
                    yield return nested;
                }
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                foreach (var nested in FindPlaceNodes(property.Value))
                {
                    yield return nested;
                }
            }
        }
    }

    private static bool IsPlaceNode(IReadOnlyList<JsonElement> nodeValues)
    {
        if (nodeValues.Count < 3)
        {
            return false;
        }

        var metadata = nodeValues[1];
        var nameElement = nodeValues[2];
        if (metadata.ValueKind != JsonValueKind.Array || nameElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var metadataValues = metadata.EnumerateArray().ToArray();
        if (metadataValues.Length < 6)
        {
            return false;
        }

        var coordinateElement = metadataValues[5];
        if (coordinateElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var coordinateValues = coordinateElement.EnumerateArray().ToArray();
        if (coordinateValues.Length < 4)
        {
            return false;
        }

        return coordinateValues[2].ValueKind == JsonValueKind.Number && coordinateValues[3].ValueKind == JsonValueKind.Number;
    }

    private static SavedPlace MapPlace(JsonElement node)
    {
        var values = node.EnumerateArray().ToArray();
        var metadataValues = values[1].EnumerateArray().ToArray();
        var coordinateValues = metadataValues[5].EnumerateArray().ToArray();

        var place = new SavedPlace
        {
            Name = values[2].GetString() ?? string.Empty,
            Address = metadataValues.Length > 2 && metadataValues[2].ValueKind == JsonValueKind.String
                ? metadataValues[2].GetString()
                : null,
            Latitude = coordinateValues[2].GetDouble(),
            Longitude = coordinateValues[3].GetDouble(),
            Note = values.Length > 3 && values[3].ValueKind == JsonValueKind.String ? values[3].GetString() : null,
            ImageUrl = TryReadImage(values)
        };

        return place;
    }

    private static string? TryReadImage(IReadOnlyList<JsonElement> values)
    {
        if (values.Count <= 17)
        {
            return null;
        }

        var contributorElement = values[17];
        if (contributorElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var contributorValues = contributorElement.EnumerateArray().ToArray();
        if (contributorValues.Length < 2)
        {
            return null;
        }

        return contributorValues[1].ValueKind == JsonValueKind.String ? contributorValues[1].GetString() : null;
    }
}
