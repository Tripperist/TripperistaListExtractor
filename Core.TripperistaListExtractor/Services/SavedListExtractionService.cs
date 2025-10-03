using Core.TripperistaListExtractor.Models;
using Core.TripperistaListExtractor.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Core.TripperistaListExtractor.Services;

/// <summary>
/// Coordinates the Playwright automation necessary to capture and parse saved list data.
/// </summary>
public sealed class SavedListExtractionService(
    ILogger<SavedListExtractionService> logger,
    IPlaywrightBrowserFactory browserFactory,
    ISavedListPayloadParser payloadParser
) : ISavedListExtractionService
{
    private readonly ILogger<SavedListExtractionService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPlaywrightBrowserFactory _browserFactory = browserFactory ?? throw new ArgumentNullException(nameof(browserFactory));
    private readonly ISavedListPayloadParser _payloadParser = payloadParser ?? throw new ArgumentNullException(nameof(payloadParser));

    /// <inheritdoc />
    public async Task<SavedList> ExtractAsync(Uri listUrl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listUrl);
        _logger.LogInformation(ResourceCatalog.GetLogMessage("StartingExtraction"));

        using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        await using var browser = await _browserFactory.CreateBrowserAsync(playwright, cancellationToken).ConfigureAwait(false);
        await using var context = await browser.NewContextAsync().ConfigureAwait(false);
        var page = await context.NewPageAsync().ConfigureAwait(false);

        await page.GotoAsync(listUrl.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }).ConfigureAwait(false);
        await EnsureListFullyLoadedAsync(page, cancellationToken).ConfigureAwait(false);

        var scriptPayload = await LocatePayloadAsync(page, cancellationToken).ConfigureAwait(false);
        await page.CloseAsync().ConfigureAwait(false);
        return _payloadParser.Parse(scriptPayload);
    }

    private async Task EnsureListFullyLoadedAsync(IPage page, CancellationToken cancellationToken)
    {
        var listLocator = page.Locator("div[role=\"main\"] div.m6QErb");
        await listLocator.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached }).ConfigureAwait(false);

        double previousHeight = 0;
        while (true)
        {
            _logger.LogDebug(ResourceCatalog.GetLogMessage("ScrollingList"));
            await listLocator.Last.EvaluateAsync("element => element.scrollTo(0, element.scrollHeight)").ConfigureAwait(false);
            await page.WaitForTimeoutAsync(500);
            var currentHeight = await listLocator.Last.EvaluateAsync<double>("element => element.scrollHeight").ConfigureAwait(false);
            if (Math.Abs(currentHeight - previousHeight) < 1)
            {
                break;
            }

            previousHeight = currentHeight;
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task<string> LocatePayloadAsync(IPage page, CancellationToken cancellationToken)
    {
        var scriptLocator = page.Locator("head script");
        if (await scriptLocator.CountAsync().ConfigureAwait(false) < 2)
        {
            throw new InvalidOperationException(ResourceCatalog.GetErrorMessage("ScriptNotFound"));
        }

        var scriptContent = await scriptLocator.Nth(1).InnerTextAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        const string prefix = ")]}'\\n";
        const string suffix = "\\u003d13\"]";
        var startIndex = scriptContent.IndexOf(prefix, StringComparison.Ordinal);
        if (startIndex < 0)
        {
            throw new InvalidOperationException(ResourceCatalog.GetErrorMessage("ScriptParsingFailed"));
        }

        startIndex += prefix.Length;
        var endIndex = scriptContent.IndexOf(suffix, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            endIndex = scriptContent.Length;
        }

        _logger.LogDebug(ResourceCatalog.GetLogMessage("ScriptLocated"));
        return scriptContent.Substring(startIndex, endIndex - startIndex + suffix.Length);
    }
}
