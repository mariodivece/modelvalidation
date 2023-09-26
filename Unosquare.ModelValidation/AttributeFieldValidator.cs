using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace Unosquare.ModelValidation;

public class AttributeFieldValidator : IFieldValidator
{
    public AttributeFieldValidator(PropertyInfo propertyInfo, ValidationAttribute attributeInstance)
    {
        Property = propertyInfo;
        Attribute = attributeInstance;
        ModelType = propertyInfo.DeclaringType ?? typeof(object);
    }

    public Type ModelType { get; }

    public ValidationAttribute Attribute { get; }

    public string FieldName => Property.Name;

    public PropertyInfo Property { get; }

    public ValidationResult? Validate(object? instance, IStringLocalizer? localizer = null)
    {
        var propertyValue = Property.GetValue(instance);
        var errorMessage = string.Empty;

        if (!string.IsNullOrWhiteSpace(Attribute.ErrorMessageResourceName) &&
            Attribute.ErrorMessageResourceType is null &&
            localizer is not null)
        {
            var resource = localizer.GetString(Attribute.ErrorMessageResourceName);
            if (resource is not null && !resource.ResourceNotFound)
            {
                Attribute.ErrorMessageResourceType = null;
                Attribute.ErrorMessageResourceName = null;
                Attribute.ErrorMessage = resource.Value;
            }
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
            errorMessage = Attribute.FormatErrorMessage(FieldName);
        
        return Attribute.IsValid(propertyValue)
            ? ValidationResult.Success
            : new ValidationResult(errorMessage);
    }

    public ValueTask<ValidationResult?> ValidateAsync(object? instance, IStringLocalizer? localizer = null)
    {
        var result = Validate(instance, localizer);
        return ValueTask.FromResult(result);
    }
}
