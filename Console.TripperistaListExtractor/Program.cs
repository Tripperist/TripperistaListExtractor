using System.CommandLine;
using System.Linq;
using Console.TripperistaListExtractor.Commands;
using Core.TripperistaListExtractor.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Service.TripperistaListExtractor.Contracts;
using Service.TripperistaListExtractor.Implementations;
using Service.TripperistaListExtractor.Parsers;
using Service.TripperistaListExtractor.SemanticKernel;
using Service.TripperistaListExtractor.Writers;

var verboseRequested = args.Any(arg => string.Equals(arg, "--verbose", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-v", StringComparison.OrdinalIgnoreCase));

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(verboseRequested ? LogLevel.Debug : LogLevel.Information);

builder.Services.AddSingleton<ISavedListPayloadParser, SavedListPayloadParser>();
builder.Services.AddSingleton<IFileWriterFactory, FileWriterFactory>();
builder.Services.AddSingleton<IAiMetadataSanitizer, SemanticKernelMetadataSanitizer>();
builder.Services.AddSingleton<IGoogleMapsListExtractorService, GoogleMapsListExtractorService>();
builder.Services.AddSingleton<ListExtractionCommandHandler>();

using var host = builder.Build();

var rootCommand = BuildRootCommand(host.Services);
return await rootCommand.InvokeAsync(args);

static RootCommand BuildRootCommand(IServiceProvider services)
{
    var inputUrlOption = new Option<string>("--inputSavedListUrl", description: "The Google Maps saved list share URL.")
    {
        IsRequired = true,
    };

    var outputKmlOption = new Option<string?>("--outputKmlFile", description: "Optional KML output file name.");
    var outputCsvOption = new Option<string?>("--outputCsvFile", description: "Optional CSV output file name.");
    var apiKeyOption = new Option<string?>("--googlePlacesApiKey", description: "Optional Google Places API key override.");
    var verboseOption = new Option<bool>("--verbose", description: "Enables verbose logging output.");
    verboseOption.AddAlias("-v");
    var headlessOption = new Option<bool>("--headless", description: "Runs the Playwright browser in headless mode.")
    {
        Arity = ArgumentArity.ZeroOrOne,
        DefaultValueFactory = _ => true,
    };

    var root = new RootCommand("Extracts Google Maps saved lists to CSV and KML.")
    {
        inputUrlOption,
        outputKmlOption,
        outputCsvOption,
        apiKeyOption,
        verboseOption,
        headlessOption,
    };

    root.SetHandler(async (string inputUrl, string? kml, string? csv, string? apiKey, bool verbose, bool headless) =>
    {
        using var scope = services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ListExtractionCommandHandler>();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        var options = new ExtractionOptions
        {
            InputSavedListUrl = inputUrl,
            OutputKmlFile = string.IsNullOrWhiteSpace(kml) ? null : kml,
            OutputCsvFile = string.IsNullOrWhiteSpace(csv) ? null : csv,
            GooglePlacesApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
            Verbose = verbose,
            Headless = headless,
        };

        await handler.HandleAsync(options, cts.Token).ConfigureAwait(false);
    }, inputUrlOption, outputKmlOption, outputCsvOption, apiKeyOption, verboseOption, headlessOption);

    return root;
}
