namespace Service.TripperistaListExtractor.SemanticKernel;

/// <summary>
/// Provides semantic sanitization for metadata strings.
/// </summary>
public interface IAiMetadataSanitizer
{
    /// <summary>
    /// Produces a deterministic, filesystem-safe filename fragment derived from <paramref name="seed"/>.
    /// </summary>
    /// <param name="seed">The seed string to sanitize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A sanitized filename fragment.</returns>
    ValueTask<string> SanitizeFilenameAsync(string seed, CancellationToken cancellationToken);
}
