namespace Core.TripperistaListExtractor.Commands;

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Resources;

/// <summary>
///     Provides a reusable template for executing console commands using strongly-typed option payloads.
/// </summary>
/// <typeparam name="TOptions">The option type that is validated and consumed by the command.</typeparam>
public abstract class CommandHandler<TOptions>
    where TOptions : class, new()
{
    /// <summary>
    ///     Initialises a new instance of the <see cref="CommandHandler{TOptions}"/> class.
    /// </summary>
    /// <param name="logResourceManager">The resource manager that provides localised log messages.</param>
    /// <param name="errorResourceManager">The resource manager that provides localised error messages.</param>
    protected CommandHandler(ResourceManager logResourceManager, ResourceManager errorResourceManager)
    {
        LogResourceManager = logResourceManager ?? throw new ArgumentNullException(nameof(logResourceManager));
        ErrorResourceManager = errorResourceManager ?? throw new ArgumentNullException(nameof(errorResourceManager));
    }

    /// <summary>
    ///     Gets the resource manager that resolves log messages for derived handlers.
    /// </summary>
    protected ResourceManager LogResourceManager { get; }

    /// <summary>
    ///     Gets the resource manager that resolves error messages for derived handlers.
    /// </summary>
    protected ResourceManager ErrorResourceManager { get; }

    /// <summary>
    ///     Executes the command using the provided option payload and cancellation token.
    /// </summary>
    /// <param name="options">The bound option set supplied by the command line.</param>
    /// <param name="cancellationToken">The cancellation token associated with the hosting infrastructure.</param>
    /// <returns>A task that resolves to the process exit code.</returns>
    public async Task<int> ExecuteAsync(TOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);
        return await ExecuteInternalAsync(options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Performs the option-specific validation using <see cref="Validator"/> and the <see cref="ValidationContext"/>.
    /// </summary>
    /// <param name="options">The option payload to validate.</param>
    protected virtual void ValidateOptions(TOptions options)
    {
        var validationContext = new ValidationContext(options, serviceProvider: null, items: null);
        Validator.ValidateObject(options, validationContext, validateAllProperties: true);
    }

    /// <summary>
    ///     Deriving classes must implement the command logic that returns an exit code.
    /// </summary>
    /// <param name="options">The validated option payload.</param>
    /// <param name="cancellationToken">The cancellation token that propagates shutdown requests.</param>
    /// <returns>A task representing the asynchronous execution result.</returns>
    protected abstract Task<int> ExecuteInternalAsync(TOptions options, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves a localised log message for the supplied key.
    /// </summary>
    /// <param name="resourceKey">The resource key to resolve.</param>
    /// <returns>The formatted localised message.</returns>
    protected string GetLogMessage(string resourceKey)
    {
        var message = LogResourceManager.GetString(resourceKey, CultureInfo.CurrentCulture);
        return message ?? string.Empty;
    }

    /// <summary>
    ///     Retrieves a localised error message for the supplied key.
    /// </summary>
    /// <param name="resourceKey">The resource key to resolve.</param>
    /// <returns>The formatted localised message.</returns>
    protected string GetErrorMessage(string resourceKey)
    {
        var message = ErrorResourceManager.GetString(resourceKey, CultureInfo.CurrentCulture);
        return message ?? string.Empty;
    }
}
