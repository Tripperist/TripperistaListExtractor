using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using Core.TripperistaListExtractor.Commands;
using Core.TripperistaListExtractor.Options;
using Core.TripperistaListExtractor.Parsing;
using Core.TripperistaListExtractor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Console.TripperistaListExtractor;

/// <summary>
/// Entry point for the TripperistaListExtractor console application.
/// </summary>
public static class Program
{
    private static readonly Option<Uri> InputSavedListOption = new("--inputSavedListUrl", description: "URL of the Google Maps saved list.") { IsRequired = true };
    private static readonly Option<string?> OutputKmlOption = new("--outputKmlFile", () => null, "Optional KML file output path.");
    private static readonly Option<string?> OutputCsvOption = new("--outputCsvFile", () => null, "Optional CSV file output path.");
    private static readonly Option<string?> GooglePlacesApiKeyOption = new("--googlePlacesApiKey", () => null, "Google Places API key.");
    private static readonly Option<bool> VerboseOption = new("--verbose", () => false, "Enable verbose logging.");

    public static async Task<int> Main(string[] args)
    {
        var verboseRequested = args.Contains("--verbose", StringComparer.OrdinalIgnoreCase);

        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                              .AddEnvironmentVariables();

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(verboseRequested ? LogLevel.Debug : LogLevel.Warning);

        builder.Services.AddSingleton<IPlaywrightBrowserFactory, PlaywrightBrowserFactory>();
        builder.Services.AddSingleton<ISavedListPayloadParser, SavedListPayloadParser>();
        builder.Services.AddSingleton<ISavedListExtractionService, SavedListExtractionService>();
        builder.Services.AddSingleton<ICsvExporter, CsvExporter>();
        builder.Services.AddSingleton<IKmlExporter, KmlExporter>();
        builder.Services.AddSingleton<ExtractListCommandHandler>();

        await using var host = builder.Build();

        var rootCommand = CreateRootCommand();
        rootCommand.SetHandler(async (InvocationContext context) =>
        {
            var options = BindOptions(context, host.Services.GetRequiredService<IConfiguration>());
            var handler = host.Services.GetRequiredService<ExtractListCommandHandler>();
            var exitCode = await handler.ExecuteAsync(options, context.GetCancellationToken()).ConfigureAwait(false);
            context.ExitCode = exitCode;
        });

        return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
    }

    private static RootCommand CreateRootCommand()
    {
        var command = new RootCommand("Extracts Google Maps saved list data and exports CSV/KML artifacts.");

        command.AddOption(InputSavedListOption);
        command.AddOption(OutputKmlOption);
        command.AddOption(OutputCsvOption);
        command.AddOption(GooglePlacesApiKeyOption);
        command.AddOption(VerboseOption);

        return command;
    }

    private static ExtractListCommandOptions BindOptions(InvocationContext context, IConfiguration configuration)
    {
        var parseResult = context.ParseResult;
        var options = new ExtractListCommandOptions
        {
            InputSavedListUrl = parseResult.GetValueForOption(InputSavedListOption),
            OutputKmlFile = parseResult.GetValueForOption(OutputKmlOption),
            OutputCsvFile = parseResult.GetValueForOption(OutputCsvOption),
            GooglePlacesApiKey = parseResult.GetValueForOption(GooglePlacesApiKeyOption),
            Verbose = parseResult.GetValueForOption(VerboseOption)
        };

        options.GooglePlacesApiKey ??= configuration["GooglePlaces:ApiKey"] ?? Environment.GetEnvironmentVariable("GOOGLE_PLACES_API_KEY");
        return options;
    }
}
