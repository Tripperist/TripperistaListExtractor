using System.Resources;

namespace Console.TripperistaListExtractor.Resources;

/// <summary>
/// Provides resource managers for localized console strings.
/// </summary>
internal static class ResourceCatalog
{
    /// <summary>
    /// Gets the error resource manager.
    /// </summary>
    internal static ResourceManager Errors { get; } = new(
        "Console.TripperistaListExtractor.Resources.ConsoleErrorMessages",
        typeof(ResourceCatalog).Assembly);

    /// <summary>
    /// Gets the log resource manager.
    /// </summary>
    internal static ResourceManager Logs { get; } = new(
        "Console.TripperistaListExtractor.Resources.ConsoleLogMessages",
        typeof(ResourceCatalog).Assembly);

    /// <summary>
    /// Gets the help resource manager.
    /// </summary>
    internal static ResourceManager Help { get; } = new(
        "Console.TripperistaListExtractor.Resources.ConsoleHelpMessages",
        typeof(ResourceCatalog).Assembly);
}
}
