using System.IO;
using System.Text;
using Service.TripperistaListExtractor.Contracts;

namespace Service.TripperistaListExtractor.Implementations;

/// <summary>
/// Generates safe file names by stripping invalid characters and normalizing whitespace.
/// </summary>
public sealed class FileNameGenerator : IFileNameGenerator
{
    /// <inheritdoc />
    public string Generate(string baseName, string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        // Normalize the base name by trimming, collapsing whitespace, and removing invalid path characters.
        var trimmed = baseName.Trim();
        if (trimmed.Length == 0)
        {
            trimmed = "output";
        }

        var builder = new StringBuilder();
        var lastWasWhitespace = false;
        foreach (var rune in trimmed.EnumerateRunes())
        {
            if (char.IsWhiteSpace(rune.Value))
            {
                if (!lastWasWhitespace)
                {
                    // Use a hyphen separator so that spaces do not produce unreadable file names.
                    builder.Append('-');
                    lastWasWhitespace = true;
                }

                continue;
            }

            lastWasWhitespace = false;
            if (Array.IndexOf(Path.GetInvalidFileNameChars(), rune.Value) >= 0)
            {
                // Skip characters that would raise IO exceptions when the file is materialised.
                continue;
            }

            builder.Append(rune.ToString().Normalize(NormalizationForm.FormKC));
        }

        var sanitized = builder.Length == 0 ? "output" : builder.ToString().Trim('-');
        if (sanitized.Length == 0)
        {
            sanitized = "output";
        }

        // Append the extension while ensuring a leading period exists exactly once.
        var normalizedExtension = extension.StartsWith('.', StringComparison.Ordinal) ? extension : $".{extension}";
        return string.Concat(sanitized, normalizedExtension);
    }
}
