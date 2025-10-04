using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Service.TripperistaListExtractor.Parsers;

namespace Console.TripperistaListExtractor.Tests.Parsers;

/// <summary>
/// Verifies the saved list payload parser logic against representative payloads.
/// </summary>
[TestClass]
public sealed class SavedListPayloadParserTests
{
    private const string MinimalPayload = """
)]}'
[
  ["list-id",1,null,1,1],
  4,
  [2,1,"https://www.google.com/maps/placelists/list/list-id"],
  ["Test Creator","https://example.com/profile","123"],
  "",
  "Sample List",
  "Sample Description",
  null,
  null,
  [
    [
      null,
      [
        null,
        [null,null,"123 Sample St",null,"",[null,null,45.123,-93.456],["place","alt"]],
        "Sample Place",
        "Sample note",
        null,
        null,
        null,
        [],
        [],
        [],
        null,
        null,
        null,
        [],
        ["https://images.example/sample.jpg"]
      ],
      null
    ]
  ],
  "\u003d13\"]"
]
""";

    /// <summary>
    /// Ensures the parser materialises minimal payloads to domain models.
    /// </summary>
    [TestMethod]
    public void Parse_WithMinimalPayload_ReturnsSavedList()
    {
        var parser = new SavedListPayloadParser(new NullLogger<SavedListPayloadParser>());

        var savedList = parser.Parse(MinimalPayload);

        savedList.Header.Name.Should().Be("Sample List");
        savedList.Header.Description.Should().Be("Sample Description");
        savedList.Header.Creator.Should().Be("Test Creator");
        savedList.Places.Should().HaveCount(1);

        var place = savedList.Places[0];
        place.Name.Should().Be("Sample Place");
        place.Address.Should().Be("123 Sample St");
        place.Note.Should().Be("Sample note");
        place.Latitude.Should().BeApproximately(45.123, 0.001);
        place.Longitude.Should().BeApproximately(-93.456, 0.001);
        place.ImageUrl.Should().Be("https://images.example/sample.jpg");
    }
}
