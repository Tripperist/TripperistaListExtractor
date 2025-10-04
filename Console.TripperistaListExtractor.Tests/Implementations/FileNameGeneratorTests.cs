using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Service.TripperistaListExtractor.Implementations;

namespace Console.TripperistaListExtractor.Tests.Implementations;

/// <summary>
/// Verifies the behaviour of <see cref="FileNameGenerator"/>.
/// </summary>
[TestClass]
public class FileNameGeneratorTests
{
    [TestMethod]
    public void Generate_ShouldCollapseWhitespace_AndRemoveInvalidCharacters()
    {
        // Arrange
        var generator = new FileNameGenerator();

        // Act
        var result = generator.Generate(" My Invalid: Name? ", ".csv");

        // Assert
        result.Should().Be("My-Invalid-Name.csv");
    }

    [TestMethod]
    public void Generate_ShouldFallbackToDefaultName_WhenInputIsWhitespace()
    {
        // Arrange
        var generator = new FileNameGenerator();

        // Act
        var result = generator.Generate("   ", "kml");

        // Assert
        result.Should().Be("output.kml");
    }
}
