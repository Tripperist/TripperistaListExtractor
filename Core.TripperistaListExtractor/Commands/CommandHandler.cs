using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Core.TripperistaListExtractor.Commands;

/// <summary>
/// Provides a base abstraction for executing console commands with validated options.
/// </summary>
/// <typeparam name="TOptions">The validated command options type.</typeparam>
public abstract class CommandHandler<TOptions>(ILogger logger)
    where TOptions : class
{
    /// <summary>
    /// Gets the <see cref="ILogger"/> instance used for structured logging.
    /// </summary>
    protected ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Executes the command using the supplied <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The command options that have been validated.</param>
    /// <param name="cancellationToken">The cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous command execution.</returns>
    public Task HandleAsync([DisallowNull] TOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        return ExecuteCoreAsync(options, cancellationToken);
    }

    /// <summary>
    /// When overridden executes the command using the supplied options.
    /// </summary>
    /// <param name="options">The command options that have been validated.</param>
    /// <param name="cancellationToken">The cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous execution.</returns>
    protected abstract Task ExecuteCoreAsync(TOptions options, CancellationToken cancellationToken);
}
