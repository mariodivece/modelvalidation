using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Unosquare.ModelValidation;

/// <summary>
/// Defines interface members for a field validator.
/// </summary>
public interface IMemberValidator
{
    /// <summary>
    /// Gets the type holding the field for which this validator applies to.
    /// </summary>
    Type ModelType { get; }

    /// <summary>
    /// Gets the name of the target field.
    /// </summary>
    string MemberName { get; }

    /// <summary>
    /// Gets the associated property.
    /// </summary>
    PropertyInfo Member { get; }

    /// <summary>
    /// Executes validation logic within this validator.
    /// </summary>
    /// <param name="instance">The model object to validate.</param>
    /// <param name="localizer">The optional string localizer containing resources.</param>
    /// <returns>A validation result. A null instance means no error.</returns>
    ValidationResult? Validate(object? instance, IStringLocalizer? localizer = null);

    /// <summary>
    /// Executes validation logic within this validator.
    /// </summary>
    /// <param name="instance">The model object to validate.</param>
    /// <param name="localizer">The optional string localizer containing resources.</param>
    /// <returns>A validation result. A null instance means no error.</returns>
    ValueTask<ValidationResult?> ValidateAsync(object? instance, IStringLocalizer? localizer = null);
}
