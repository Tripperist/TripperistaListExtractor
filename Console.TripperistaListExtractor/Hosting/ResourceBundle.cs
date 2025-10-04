namespace Console.TripperistaListExtractor.Hosting;

using System.Resources;

/// <summary>
///     Encapsulates the resource managers required for localised log and error messages.
/// </summary>
public sealed class ResourceBundle
{
    /// <summary>
    ///     Initialises a new instance of the <see cref="ResourceBundle"/> class.
    /// </summary>
    /// <param name="logResourceManager">The resource manager responsible for log messages.</param>
    /// <param name="errorResourceManager">The resource manager responsible for error messages.</param>
    public ResourceBundle(ResourceManager logResourceManager, ResourceManager errorResourceManager)
    {
        LogResourceManager = logResourceManager ?? throw new ArgumentNullException(nameof(logResourceManager));
        ErrorResourceManager = errorResourceManager ?? throw new ArgumentNullException(nameof(errorResourceManager));
    }

    /// <summary>
    ///     Gets the resource manager that resolves log messages.
    /// </summary>
    public ResourceManager LogResourceManager { get; }

    /// <summary>
    ///     Gets the resource manager that resolves error messages.
    /// </summary>
    public ResourceManager ErrorResourceManager { get; }
}
