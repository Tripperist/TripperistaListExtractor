using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Core.TripperistaListExtractor.Services;

/// <summary>
/// Provides a resilient implementation of <see cref="IPlaywrightBrowserFactory"/>.
/// </summary>
public sealed class PlaywrightBrowserFactory(ILogger<PlaywrightBrowserFactory> logger) : IPlaywrightBrowserFactory
{
    private readonly ILogger<PlaywrightBrowserFactory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<IBrowser> CreateBrowserAsync(IPlaywright playwright, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(playwright);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Creating headless Chromium instance for scraping operations.");
        return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-gpu", "--disable-dev-shm-usage" }
        }).ConfigureAwait(false);
    }
}
