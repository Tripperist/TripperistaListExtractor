using System.Text;
using Microsoft.SemanticKernel;

namespace Service.TripperistaListExtractor.SemanticKernel;

/// <summary>
/// Provides deterministic metadata sanitization using Semantic Kernel orchestration.
/// </summary>
public sealed class SemanticKernelMetadataSanitizer : IAiMetadataSanitizer
{
    private readonly Kernel _kernel;
    private readonly KernelFunction _sanitizerFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticKernelMetadataSanitizer"/> class.
    /// </summary>
    public SemanticKernelMetadataSanitizer()
    {
        var builder = Kernel.CreateBuilder();
        _kernel = builder.Build();
        _sanitizerFunction = KernelFunctionFactory.CreateFromMethod<string, string>(
            SanitizeCore,
            functionName: "SanitizeFilename",
            description: "Produces a filesystem safe slug for file naming.");
    }

    /// <inheritdoc />
    public async ValueTask<string> SanitizeFilenameAsync(string seed, CancellationToken cancellationToken)
    {
        var arguments = new KernelArguments
        {
            ["input"] = seed,
        };

        var result = await _kernel.InvokeAsync(_sanitizerFunction, arguments, cancellationToken)
            .ConfigureAwait(false);

        return result.GetValue<string>() ?? "export";
    }

    private static string SanitizeCore(string seed)
    {
        var trimmed = seed.AsSpan().Trim();
        if (trimmed.IsEmpty)
        {
            return "export";
        }

        Span<char> buffer = stackalloc char[Math.Min(trimmed.Length, 64)];
        var builder = new StringBuilder(buffer.Length);
        foreach (var ch in trimmed)
        {
            var safe = char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ch switch
            {
                ' ' or '-' or '_' => '-',
                _ => default,
            };

            if (safe == default)
            {
                continue;
            }

            if (safe == '-')
            {
                if (builder.Length == 0 || builder[^1] == '-')
                {
                    continue;
                }
            }

            builder.Append(safe);
            if (builder.Length == buffer.Length)
            {
                break;
            }
        }

        return builder.Length == 0 ? "export" : builder.ToString().Trim('-');
    }
}
