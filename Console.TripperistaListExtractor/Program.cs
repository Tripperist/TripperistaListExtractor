using Console.TripperistaListExtractor.Commands;
using Console.TripperistaListExtractor.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Service.TripperistaListExtractor.Contracts;
using Service.TripperistaListExtractor.Implementations;
using Service.TripperistaListExtractor.Parsers;
using Service.TripperistaListExtractor.Writers;

// Infer verbose logging before we spin up the host so the logging pipeline is configured correctly for diagnostics.
var verboseRequested = DetermineVerboseRequested(args);

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
builder.Services.AddSingleton<IFileNameGenerator, FileNameGenerator>();
builder.Services.AddSingleton<IGoogleMapsListExtractorService, GoogleMapsListExtractorService>();
builder.Services.AddSingleton<ICommandLineParser, CommandLineParser>();
builder.Services.AddSingleton<ListExtractionCommandHandler>();

using var host = builder.Build();
var parser = host.Services.GetRequiredService<ICommandLineParser>();
// Parse the command line using the bespoke parser to avoid System.CommandLine version drift.
var parseResult = parser.Parse(args);

if (parseResult.ShowHelp)
{
    System.Console.WriteLine(parser.GetUsage());
    return 0;
}

if (!string.IsNullOrWhiteSpace(parseResult.ErrorMessage))
{
    System.Console.Error.WriteLine(parseResult.ErrorMessage);
    System.Console.WriteLine(parser.GetUsage());
    return 1;
}

var options = parseResult.Options ?? throw new InvalidOperationException("Parser returned null options without error.");

using var scope = host.Services.CreateScope();
var handler = scope.ServiceProvider.GetRequiredService<ListExtractionCommandHandler>();
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

await handler.HandleAsync(options, cts.Token).ConfigureAwait(false);
return 0;

static bool DetermineVerboseRequested(string[] args)
{
    for (var index = 0; index < args.Length; index++)
    {
        var token = args[index];
        // Look for the verbose switch and honour explicit true/false arguments so scripted invocations can toggle output levels.
        if (!string.Equals(token, "--verbose", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(token, "-v", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        var nextIndex = index + 1;
        if (nextIndex >= args.Length || args[nextIndex].StartsWith("-", StringComparison.Ordinal))
        {
            return true;
        }

        if (bool.TryParse(args[nextIndex], out var value))
        {
            return value;
        }

        return true;
    }

    return false;
}
