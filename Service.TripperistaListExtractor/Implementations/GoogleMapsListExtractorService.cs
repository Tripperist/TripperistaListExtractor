namespace Service.TripperistaListExtractor.Implementations;

using System.Text;
using System.Text.Json;
using Core.TripperistaListExtractor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Service.TripperistaListExtractor.Contracts;

/// <summary>
///     Uses Microsoft Playwright to scrape Google Maps saved list pages and build domain models.
/// </summary>
public sealed class GoogleMapsListExtractorService(ILogger<GoogleMapsListExtractorService> logger, ISavedListPayloadParser payloadParser)
    : IGoogleMapsListExtractorService
{
    private const string PayloadStartMarker = ")]}'\n";
    private const string PayloadEndMarker = "\\u003d13\"]";

    private readonly ILogger<GoogleMapsListExtractorService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ISavedListPayloadParser _payloadParser = payloadParser ?? throw new ArgumentNullException(nameof(payloadParser));

    /// <inheritdoc />
    public async Task<SavedList> ExtractAsync(Uri listUri, bool verbose, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listUri);

        using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-dev-shm-usage", "--no-sandbox" }
        }).ConfigureAwait(false);

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = null
        }).ConfigureAwait(false);

        var page = await context.NewPageAsync().ConfigureAwait(false);

        if (verbose)
        {
            page.Console += (_, message) => _logger.LogDebug("Playwright console: {Text}", message.Text);
            page.PageError += (_, message) => _logger.LogWarning("Page error: {Message}", message);
        }

        _logger.LogInformation("Navigating to {Uri}", listUri);
        await page.GotoAsync(listUri.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60000
        }).ConfigureAwait(false);

        await page.WaitForSelectorAsync("div[role=\"main\"]", new PageWaitForSelectorOptions
        {
            Timeout = 60000
        }).ConfigureAwait(false);

        await EnsureListFullyLoadedAsync(page, cancellationToken).ConfigureAwait(false);

        var scriptContent = await page.EvaluateAsync<string>(@"() => {
            const scripts = Array.from(document.querySelectorAll('head > script'));
            if (scripts.length < 2) {
                return '';
            }
            const target = scripts[1];
            return target.textContent ?? '';
        }").ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(scriptContent))
        {
            throw new InvalidOperationException("The expected payload script could not be located.");
        }

        var payload = ExtractPayload(scriptContent);
        var parsed = await page.EvaluateAsync<JsonElement>("payload => JSON.parse(payload)", payload).ConfigureAwait(false);

        return _payloadParser.Parse(parsed);
    }

    private static async Task EnsureListFullyLoadedAsync(IPage page, CancellationToken cancellationToken)
    {
        var scrollableHandle = await page.QuerySelectorAsync("div[role=\"main\"] div.m6QErb.DxyBCb.kA9KIf.dS8AEf.XiKgde.ussYcc").ConfigureAwait(false);
        if (scrollableHandle is null)
        {
            return;
        }

        var iterations = 0;
        while (iterations < 200)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scrolled = await scrollableHandle.EvaluateAsync<bool>(@"async element => {
                const previous = element.scrollTop;
                element.scrollBy(0, element.clientHeight);
                await new Promise(resolve => setTimeout(resolve, 350));
                return element.scrollTop !== previous;
            }").ConfigureAwait(false);

            if (!scrolled)
            {
                break;
            }

            iterations++;
        }
    }

    private static string ExtractPayload(string scriptContent)
    {
        var startIndex = scriptContent.IndexOf(PayloadStartMarker, StringComparison.Ordinal);
        if (startIndex < 0)
        {
            throw new InvalidOperationException("The payload start marker was not found.");
        }

        startIndex += PayloadStartMarker.Length;
        var endIndex = scriptContent.IndexOf(PayloadEndMarker, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            throw new InvalidOperationException("The payload end marker was not found.");
        }

        var rawPayload = scriptContent.Substring(startIndex, endIndex - startIndex + PayloadEndMarker.Length);
        return SanitizePayload(rawPayload);
    }

    private static string SanitizePayload(string rawPayload)
    {
        if (string.IsNullOrEmpty(rawPayload))
        {
            return rawPayload;
        }

        var builder = new StringBuilder(rawPayload.Length);
        for (var index = 0; index < rawPayload.Length; index++)
        {
            var character = rawPayload[index];
            if (character == '\\' && index + 1 < rawPayload.Length)
            {
                var lookahead = rawPayload[index + 1];
                if (lookahead == '\\' || lookahead == '"')
                {
                    builder.Append(lookahead);
                    index++;
                    continue;
                }
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
