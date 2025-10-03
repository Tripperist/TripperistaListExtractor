using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Core.TripperistaListExtractor.Commands;

/// <summary>
/// Provides a template for command handlers that encapsulate validation and execution orchestration.
/// </summary>
/// <typeparam name="TOptions">The type containing the command options to validate and execute.</typeparam>
public abstract class CommandHandler<TOptions>(ILogger logger)
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Executes the command using the supplied options.
    /// </summary>
    /// <param name="options">The options describing the command invocation.</param>
    /// <param name="cancellationToken">Propagates cancellation notifications.</param>
    /// <returns>An exit code that indicates success (<c>0</c>) or failure (non-zero).</returns>
    public async Task<int> ExecuteAsync(TOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);
        return await ExecuteCoreAsync(options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Provides subclasses an opportunity to incorporate custom validation rules.
    /// </summary>
    /// <param name="options">The options being validated.</param>
    protected virtual void ValidateOptions(TOptions options)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(options!);
        if (!Validator.TryValidateObject(options!, validationContext, validationResults, validateAllProperties: true))
        {
            foreach (var result in validationResults)
            {
                _logger.LogError(result.ErrorMessage);
            }

            throw new ValidationException(ResourceCatalog.GetErrorMessage("MissingListUrl"));
        }
    }

    /// <summary>
    /// Derived types must implement the actual command logic.
    /// </summary>
    /// <param name="options">The validated options object.</param>
    /// <param name="cancellationToken">Propagates cancellation notifications.</param>
    /// <returns>An exit code value.</returns>
    protected abstract Task<int> ExecuteCoreAsync(TOptions options, CancellationToken cancellationToken);
}
