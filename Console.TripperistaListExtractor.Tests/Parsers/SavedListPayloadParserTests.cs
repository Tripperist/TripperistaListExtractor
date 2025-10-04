namespace Console.TripperistaListExtractor.Tests.Parsers;

using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Service.TripperistaListExtractor.Parsers;

/// <summary>
///     Provides regression coverage for the <see cref="SavedListPayloadParser"/> component.
/// </summary>
[TestClass]
public sealed class SavedListPayloadParserTests
{
    /// <summary>
    ///     Ensures the parser can map a simplified payload into strongly typed objects.
    /// </summary>
    [TestMethod]
    public void Parse_ShouldPopulateSavedList()
    {
        const string json = """
        [
            "Sample List",
            "A friendly description",
            null,
            null,
            [
                [
                    null,
                    [
                        null,
                        null,
                        "123 Example Street",
                        null,
                        "",
                        [null, null, 51.1234, -0.5678]
                    ],
                    "Example Place",
                    "Remember to visit",
                    null,
                    null,
                    null,
                    [],
                    [],
                    [],
                    [],
                    null,
                    ["Creator", "https://example.com/avatar.png", "id"]
                ]
            ]
        ]
        """;

        using var document = JsonDocument.Parse(json);
        var parser = new SavedListPayloadParser();

        var result = parser.Parse(document.RootElement);

        result.Should().NotBeNull();
        result.Header.Name.Should().Be("Sample List");
        result.Header.Description.Should().Be("A friendly description");
        result.Places.Should().HaveCount(1);

        var place = result.Places[0];
        place.Name.Should().Be("Example Place");
        place.Address.Should().Be("123 Example Street");
        place.Note.Should().Be("Remember to visit");
        place.Latitude.Should().BeApproximately(51.1234, 0.0001);
        place.Longitude.Should().BeApproximately(-0.5678, 0.0001);
    }
}
