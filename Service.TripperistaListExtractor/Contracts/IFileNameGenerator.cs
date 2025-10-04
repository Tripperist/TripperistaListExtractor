namespace Service.TripperistaListExtractor.Contracts;

/// <summary>
/// Provides a strategy for converting arbitrary names into safe file names.
/// </summary>
public interface IFileNameGenerator
{
    /// <summary>
    /// Produces a sanitized file name that uses the supplied <paramref name="baseName"/> and
    /// <paramref name="extension"/>.
    /// </summary>
    /// <param name="baseName">The logical name to convert into a file-system safe representation.</param>
    /// <param name="extension">The extension to append, including the leading period.</param>
    /// <returns>A sanitized file name that can be safely used on the local file system.</returns>
    string Generate(string baseName, string extension);
}
