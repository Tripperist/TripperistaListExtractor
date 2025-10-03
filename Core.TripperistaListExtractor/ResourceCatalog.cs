using System.Resources;

namespace Core.TripperistaListExtractor;

/// <summary>
/// Provides strongly-typed accessors for localized resources.
/// </summary>
public static class ResourceCatalog
{
    private static readonly ResourceManager LogResourceManager = new("Core.TripperistaListExtractor.Resources.LogMessages", typeof(ResourceCatalog).Assembly);
    private static readonly ResourceManager ErrorResourceManager = new("Core.TripperistaListExtractor.Resources.ErrorMessages", typeof(ResourceCatalog).Assembly);

    /// <summary>
    /// Retrieves a localized log message template.
    /// </summary>
    /// <param name="resourceKey">The key identifying the desired resource value.</param>
    /// <returns>The localized message template or the key when no resource was located.</returns>
    public static string GetLogMessage(string resourceKey) => LogResourceManager.GetString(resourceKey) ?? resourceKey;

    /// <summary>
    /// Retrieves a localized error message template.
    /// </summary>
    /// <param name="resourceKey">The key identifying the desired resource value.</param>
    /// <returns>The localized error template or the key when no resource was located.</returns>
    public static string GetErrorMessage(string resourceKey) => ErrorResourceManager.GetString(resourceKey) ?? resourceKey;
}
