using System.ComponentModel.DataAnnotations;

namespace Core.TripperistaListExtractor.Configuration;

/// <summary>
/// Validates that a string contains non-whitespace characters.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class NotEmptyOrWhitespaceAttribute : ValidationAttribute
{
    /// <inheritdoc />
    public override bool IsValid(object? value)
        => value switch
        {
            null => true,
            string text => !string.IsNullOrWhiteSpace(text),
            _ => false,
        };
}
