using Microsoft.Playwright;

namespace Core.TripperistaListExtractor.Services;

/// <summary>
/// Creates and configures Playwright browser instances using the factory pattern.
/// </summary>
public interface IPlaywrightBrowserFactory
{
    /// <summary>
    /// Creates a Playwright browser instance configured for the extraction workflow.
    /// </summary>
    /// <param name="playwright">The Playwright runtime.</param>
    /// <param name="cancellationToken">Propagates cancellation notifications.</param>
    /// <returns>A task that resolves to a configured <see cref="IBrowser"/>.</returns>
    Task<IBrowser> CreateBrowserAsync(IPlaywright playwright, CancellationToken cancellationToken);
}
