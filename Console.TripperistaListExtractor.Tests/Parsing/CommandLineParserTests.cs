using Console.TripperistaListExtractor.Parsing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Console.TripperistaListExtractor.Tests.Parsing;

/// <summary>
/// Validates the bespoke command line parser responsible for producing extraction options.
/// </summary>
[TestClass]
public sealed class CommandLineParserTests
{
    private static CommandLineParser CreateParser()
        => new(NullLogger<CommandLineParser>.Instance);

    [TestMethod]
    public void Parse_WithHelpFlag_RequestsHelp()
    {
        // Arrange
        var parser = CreateParser();

        // Act
        var result = parser.Parse(new[] { "--help" });

        // Assert
        result.ShowHelp.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Options.Should().BeNull();
    }

    [TestMethod]
    public void Parse_WithMinimalArguments_ProducesOptions()
    {
        // Arrange
        var parser = CreateParser();
        var inputUrl = "https://maps.app.goo.gl/example";

        // Act
        var result = parser.Parse(new[] { "--inputSavedListUrl", inputUrl, "-v" });

        // Assert
        result.ErrorMessage.Should().BeNull();
        result.ShowHelp.Should().BeFalse();
        result.Options.Should().NotBeNull();
        result.Options!.InputSavedListUrl.Should().Be(inputUrl);
        result.Options.Verbose.Should().BeTrue();
    }

    [TestMethod]
    public void Parse_WithExplicitHeadlessOverride_ProducesBoolean()
    {
        // Arrange
        var parser = CreateParser();
        var inputUrl = "https://maps.app.goo.gl/example";

        // Act
        var result = parser.Parse(new[] { "--inputSavedListUrl", inputUrl, "--headless", "false" });

        // Assert
        result.Options.Should().NotBeNull();
        result.Options!.Headless.Should().BeFalse();
    }

    [TestMethod]
    public void Parse_WhenMissingRequiredUrl_ReturnsError()
    {
        // Arrange
        var parser = CreateParser();

        // Act
        var result = parser.Parse(new[] { "--verbose" });

        // Assert
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        result.Options.Should().BeNull();
    }
}
