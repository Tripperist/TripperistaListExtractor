using System.Resources;
using Console.TripperistaListExtractor.Commands;
using Console.TripperistaListExtractor.Hosting;
using Core.TripperistaListExtractor.Configuration;
using Core.TripperistaListExtractor.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Service.TripperistaListExtractor.Contracts;
using Service.TripperistaListExtractor.Implementations;
using Service.TripperistaListExtractor.Parsers;
using Service.TripperistaListExtractor.Writers;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

builder.Services
    .AddOptions<ExtractionOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations();

builder.Services.Configure<GoogleApiSettings>(builder.Configuration.GetSection("GooglePlaces"));

builder.Services.AddSingleton(new ResourceBundle(
    new ResourceManager("Console.TripperistaListExtractor.Resources.LogMessages", typeof(Program).Assembly),
    new ResourceManager("Console.TripperistaListExtractor.Resources.ErrorMessages", typeof(Program).Assembly)));

builder.Services.AddSingleton<ISavedListPayloadParser, SavedListPayloadParser>();
builder.Services.AddSingleton<IGoogleMapsListExtractorService, GoogleMapsListExtractorService>();
builder.Services.AddTransient<ICsvFileWriter, CsvFileWriter>();
builder.Services.AddTransient<IKmlFileWriter, KmlFileWriter>();
builder.Services.AddSingleton<IFileWriterFactory, FileWriterFactory>();
builder.Services.AddSingleton<ListExtractionCommandHandler>();

using var host = builder.Build();
var options = host.Services.GetRequiredService<IOptions<ExtractionOptions>>().Value;
var handler = host.Services.GetRequiredService<ListExtractionCommandHandler>();

using var cancellationSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

try
{
    await host.StartAsync(cancellationSource.Token).ConfigureAwait(false);
    var exitCode = await handler.ExecuteAsync(options, cancellationSource.Token).ConfigureAwait(false);
    Environment.ExitCode = exitCode;
}
catch (OperationCanceledException)
{
    Environment.ExitCode = -3;
}
finally
{
    await host.StopAsync().ConfigureAwait(false);
}
