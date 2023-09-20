using Microsoft.Extensions.Localization;
using System.Reflection;

namespace Unosquare.ModelValidation;

public interface IFieldValidator
{
    string FieldName { get; }

    PropertyInfo Property { get; }

    FieldValidationResult Validate(object instance, IStringLocalizer? localizer = null);

    ValueTask<FieldValidationResult> ValidateAsync(object instance, IStringLocalizer? localizer = null);
}
