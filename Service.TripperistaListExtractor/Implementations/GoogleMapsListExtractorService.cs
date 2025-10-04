using System.Collections.Generic;
using System.Globalization;
using Core.TripperistaListExtractor.Models;
using Core.TripperistaListExtractor.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Service.TripperistaListExtractor.Contracts;
using Service.TripperistaListExtractor.Resources;

namespace Service.TripperistaListExtractor.Implementations;

/// <summary>
/// Coordinates the Google Maps saved list extraction workflow by navigating the page, locating the
/// payload script, parsing the payload, and finally persisting the results using the requested writers.
/// </summary>
public sealed class GoogleMapsListExtractorService(
    ILogger<GoogleMapsListExtractorService> logger,
    ISavedListPayloadParser payloadParser,
    IFileWriterFactory fileWriterFactory,
    IFileNameGenerator fileNameGenerator) : IGoogleMapsListExtractorService
{
    private readonly ILogger<GoogleMapsListExtractorService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ISavedListPayloadParser _payloadParser = payloadParser ?? throw new ArgumentNullException(nameof(payloadParser));
    private readonly IFileWriterFactory _fileWriterFactory = fileWriterFactory ?? throw new ArgumentNullException(nameof(fileWriterFactory));
    private readonly IFileNameGenerator _fileNameGenerator = fileNameGenerator ?? throw new ArgumentNullException(nameof(fileNameGenerator));

    /// <inheritdoc />
    public async Task<SavedList> ExtractAsync(ExtractionOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Log the start of the extraction process to aid diagnostics, especially when multiple URLs are processed sequentially.
        _logger.LogInformation(ResourceCatalog.Logs.GetString("ExtractionStarted") ?? "Starting extraction for '{Url}'.", options.InputSavedListUrl);

        // Create the Playwright runtime; wrapping the disposable in a using declaration ensures timely release of unmanaged handles.
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = options.Headless,
        }).ConfigureAwait(false);

        // Creating an isolated browser context keeps cookies and cache scoped to this run, which protects against cross-run leakage.
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = CultureInfo.CurrentCulture.Name,
        }).ConfigureAwait(false);

        // Navigate directly to the list URL and ensure the network settles before we interact with DOM elements.
        var page = await context.NewPageAsync().ConfigureAwait(false);
        var response = await page.GotoAsync(options.InputSavedListUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 120_000,
        }).ConfigureAwait(false);

        if (response is null || !response.Ok)
        {
            throw new InvalidOperationException(ResourceCatalog.Errors.GetString("PlaywrightNavigationFailed"));
        }

        // Scroll the list container to force lazy-loaded tiles to render before we attempt to parse their data.
        await EnsureListFullyLoadedAsync(page, cancellationToken).ConfigureAwait(false);

        // Extract and parse the embedded payload, which holds the structured list metadata and place entries.
        var payload = await ExtractPayloadAsync(page).ConfigureAwait(false);
        var savedList = _payloadParser.Parse(payload);

        // Determine which filenames to use for persistence, falling back to sanitized list names when arguments are absent.
        var csvPath = ResolveOutputPath(options.OutputCsvFile, savedList.Header.Name, ".csv");
        var kmlPath = ResolveOutputPath(options.OutputKmlFile, savedList.Header.Name, ".kml");

        // Kick off the file writing tasks in parallel so that large lists complete faster when both formats are requested.
        var persistenceTasks = new List<Task>();
        if (csvPath is not null)
        {
            _logger.LogDebug(ResourceCatalog.Logs.GetString("PersistingCsv") ?? "Writing CSV to '{Csv}'.", csvPath);
            persistenceTasks.Add(_fileWriterFactory.CreateCsv(csvPath).WriteAsync(savedList, cancellationToken));
        }

        if (kmlPath is not null)
        {
            _logger.LogDebug(ResourceCatalog.Logs.GetString("PersistingKml") ?? "Writing KML to '{Kml}'.", kmlPath);
            persistenceTasks.Add(_fileWriterFactory.CreateKml(kmlPath).WriteAsync(savedList, cancellationToken));
        }

        await Task.WhenAll(persistenceTasks).ConfigureAwait(false);

        // With persistence finished, record a completion event including summary statistics for observability.
        _logger.LogInformation(ResourceCatalog.Logs.GetString("ExtractionCompleted") ?? "Finished extraction for '{ListName}' with {PlaceCount} places.", savedList.Header.Name, savedList.Places.Count);
        return savedList;
    }

    private string? ResolveOutputPath(string? candidate, string listName, string extension)
    {
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            return candidate;
        }

        if (string.IsNullOrWhiteSpace(listName))
        {
            return _fileNameGenerator.Generate("SavedList", extension);
        }

        return _fileNameGenerator.Generate(listName, extension);
    }

    private async Task EnsureListFullyLoadedAsync(IPage page, CancellationToken cancellationToken)
    {
        const int maxIterations = 40;
        var previousHeight = 0d;

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogDebug(ResourceCatalog.Logs.GetString("PlaywrightScrolling") ?? "Scrolling iteration {Iteration} to load additional places.", iteration + 1);

            // Scroll the main container to the bottom; when the height stops changing we can assume the list is loaded.
            var currentHeight = await page.EvaluateAsync<double>(
                @"() => {
                    const main = document.querySelector('div[role="main"]');
                    if (!main) {
                        return 0;
                    }
                    const { scrollHeight } = main;
                    main.scrollTo({ top: scrollHeight, behavior: 'instant' });
                    return scrollHeight;
                }").ConfigureAwait(false);

            if (Math.Abs(currentHeight - previousHeight) < 1)
            {
                break;
            }

            previousHeight = currentHeight;
            await page.WaitForTimeoutAsync(750).ConfigureAwait(false);
        }
    }

    private async Task<string> ExtractPayloadAsync(IPage page)
    {
        const string sentinel = ")]}'\n";
        const string terminator = "\\u003d13\"]";

        // The saved list data resides in a script tag; iterate each one until the sentinel string is located.
        var scripts = page.Locator("head script");
        var count = await scripts.CountAsync().ConfigureAwait(false);
        for (var index = 0; index < count; index++)
        {
            var scriptContent = await scripts.Nth(index).InnerTextAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(scriptContent) || !scriptContent.Contains(sentinel, StringComparison.Ordinal))
            {
                continue;
            }

            // Once the sentinel is found, slice out the payload including the terminator so the parser sees the full structure.
            var sentinelIndex = scriptContent.IndexOf(sentinel, StringComparison.Ordinal);
            if (sentinelIndex < 0)
            {
                continue;
            }

            var start = sentinelIndex + sentinel.Length;
            var end = scriptContent.IndexOf(terminator, start, StringComparison.Ordinal);
            if (end < 0)
            {
                continue;
            }

            var payload = scriptContent[start..(end + terminator.Length)];
            _logger.LogDebug(ResourceCatalog.Logs.GetString("PlaywrightPayloadLocated") ?? "Located payload fragment of length {Length} characters.", payload.Length);
            return payload;
        }

        throw new InvalidOperationException(ResourceCatalog.Errors.GetString("ExtractionPayloadMissing"));
    }
}
