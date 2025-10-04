using System.Resources;

namespace Service.TripperistaListExtractor.Resources;

/// <summary>
/// Provides access to localized resource managers.
/// </summary>
internal static class ResourceCatalog
{
    /// <summary>
    /// Gets the resource manager for error messages.
    /// </summary>
    internal static ResourceManager Errors { get; } = new(
        "Service.TripperistaListExtractor.Resources.ServiceErrorMessages",
        typeof(ResourceCatalog).Assembly);

    /// <summary>
    /// Gets the resource manager for log messages.
    /// </summary>
    internal static ResourceManager Logs { get; } = new(
        "Service.TripperistaListExtractor.Resources.ServiceLogMessages",
        typeof(ResourceCatalog).Assembly);
}
