namespace Service.TripperistaListExtractor.Implementations;

using Microsoft.Extensions.DependencyInjection;
using Service.TripperistaListExtractor.Contracts;

/// <summary>
///     Provides factory methods that leverage the dependency injection container to create file writers.
/// </summary>
public sealed class FileWriterFactory(IServiceProvider serviceProvider) : IFileWriterFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public ICsvFileWriter CreateCsvWriter() => _serviceProvider.GetRequiredService<ICsvFileWriter>();

    /// <inheritdoc />
    public IKmlFileWriter CreateKmlWriter() => _serviceProvider.GetRequiredService<IKmlFileWriter>();
}
