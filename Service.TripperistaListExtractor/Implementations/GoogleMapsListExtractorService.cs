using System.Collections.Generic;
using Core.TripperistaListExtractor.Models;
using Core.TripperistaListExtractor.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Service.TripperistaListExtractor.Contracts;
using Service.TripperistaListExtractor.Resources;
using Service.TripperistaListExtractor.SemanticKernel;

namespace Service.TripperistaListExtractor.Implementations;

/// <summary>
/// Coordinates the full saved list extraction workflow using Playwright and payload parsing.
/// </summary>
public sealed class GoogleMapsListExtractorService(
    ILogger<GoogleMapsListExtractorService> logger,
    ISavedListPayloadParser payloadParser,
    IFileWriterFactory fileWriterFactory,
    IAiMetadataSanitizer metadataSanitizer) : IGoogleMapsListExtractorService
{
    private readonly ILogger<GoogleMapsListExtractorService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ISavedListPayloadParser _payloadParser = payloadParser ?? throw new ArgumentNullException(nameof(payloadParser));
    private readonly IFileWriterFactory _fileWriterFactory = fileWriterFactory ?? throw new ArgumentNullException(nameof(fileWriterFactory));
    private readonly IAiMetadataSanitizer _metadataSanitizer = metadataSanitizer ?? throw new ArgumentNullException(nameof(metadataSanitizer));

    /// <inheritdoc />
    public async Task<SavedList> ExtractAsync(ExtractionOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        _logger.LogInformation(ResourceCatalog.Logs.GetString("ExtractionStarted") ?? "Beginning extraction for list URL '{0}'.", options.InputSavedListUrl);

        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = options.Headless,
        }).ConfigureAwait(false);

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "en-US",
        }).ConfigureAwait(false);

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

        await EnsureListFullyLoadedAsync(page, cancellationToken).ConfigureAwait(false);

        var payload = await ExtractPayloadAsync(page).ConfigureAwait(false);
        var savedList = _payloadParser.Parse(payload);

        var slug = await _metadataSanitizer.SanitizeFilenameAsync(savedList.Header.Name, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug(ResourceCatalog.Logs.GetString("SemanticSanitizerUsed") ?? "AI sanitizer produced slug '{0}'.", slug);

        var csvPath = string.IsNullOrWhiteSpace(options.OutputCsvFile)
            ? $"{slug}.csv"
            : options.OutputCsvFile!;
        var kmlPath = string.IsNullOrWhiteSpace(options.OutputKmlFile)
            ? $"{slug}.kml"
            : options.OutputKmlFile!;

        var persistenceTasks = new List<Task>();
        if (!string.IsNullOrWhiteSpace(csvPath))
        {
            persistenceTasks.Add(_fileWriterFactory.CreateCsv(csvPath).WriteAsync(savedList, cancellationToken));
        }

        if (!string.IsNullOrWhiteSpace(kmlPath))
        {
            persistenceTasks.Add(_fileWriterFactory.CreateKml(kmlPath).WriteAsync(savedList, cancellationToken));
        }

        await Task.WhenAll(persistenceTasks).ConfigureAwait(false);

        _logger.LogInformation(ResourceCatalog.Logs.GetString("ExtractionCompleted") ?? "Extraction completed for list '{0}' with {1} places.", savedList.Header.Name, savedList.Places.Count);
        return savedList;
    }

    private async Task EnsureListFullyLoadedAsync(IPage page, CancellationToken cancellationToken)
    {
        const int maxIterations = 40;
        var previousHeight = 0d;

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogDebug(ResourceCatalog.Logs.GetString("PlaywrightScrolling") ?? "Scrolling to load additional list entries (iteration {0}).", iteration + 1);

            var currentHeight = await page.EvaluateAsync<double>(
                @"async () => {
                    const main = document.querySelector('div[role="main"]');
                    if (!main) {
                        return 0;
                    }
                    const { scrollHeight } = main;
                    main.scrollTo({ top: scrollHeight, behavior: 'smooth' });
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

        var scripts = await page.Locator("head script").AllInnerTextsAsync().ConfigureAwait(false);
        foreach (var script in scripts)
        {
            if (string.IsNullOrWhiteSpace(script) || !script.Contains(sentinel, StringComparison.Ordinal))
            {
                continue;
            }

            var start = script.IndexOf(sentinel, StringComparison.Ordinal) + sentinel.Length;
            var end = script.IndexOf(terminator, start, StringComparison.Ordinal);
            if (start < sentinel.Length || end < 0)
            {
                continue;
            }

            var payload = script[start..(end + terminator.Length)];
            _logger.LogDebug(ResourceCatalog.Logs.GetString("PlaywrightPayloadLocated") ?? "Located saved list payload (length {0}).", payload.Length);
            return payload;
        }

        throw new InvalidOperationException(ResourceCatalog.Errors.GetString("ExtractionPayloadMissing"));
    }
}
