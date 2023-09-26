using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Unosquare.ModelValidation;

public interface IFieldValidator
{
    Type ModelType { get; }

    string FieldName { get; }

    PropertyInfo Property { get; }

    ValidationResult? Validate(object? instance, IStringLocalizer? localizer = null);

    ValueTask<ValidationResult?> ValidateAsync(object? instance, IStringLocalizer? localizer = null);
}
