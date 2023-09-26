using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace Unosquare.ModelValidation;

/// <summary>
/// A field validator that ancapsulates a <see cref="ValidationAttribute"/>.
/// </summary>
public class MemberAttributeValidator : IMemberValidator
{
    /// <summary>
    /// Creates a new instance of the <see cref="MemberAttributeValidator"/> class.
    /// </summary>
    /// <param name="propertyInfo">The property.</param>
    /// <param name="attributeInstance">The validation attribute.</param>
    public MemberAttributeValidator(PropertyInfo propertyInfo, ValidationAttribute attributeInstance)
    {
        Member = propertyInfo;
        Attribute = attributeInstance;
        ModelType = propertyInfo?.DeclaringType ?? typeof(object);
    }

    /// <inheritdoc />
    public Type ModelType { get; }

    /// <summary>
    /// Gets the associated validation attribute instance.
    /// </summary>
    public ValidationAttribute Attribute { get; }

    /// <inheritdoc />
    public string MemberName => Member.Name;

    /// <inheritdoc />
    public PropertyInfo Member { get; }

    /// <inheritdoc />
    public ValidationResult? Validate(object? instance, IStringLocalizer? localizer = null)
    {
        var propertyValue = Member.GetValue(instance);

        if (!string.IsNullOrWhiteSpace(Attribute.ErrorMessageResourceName) &&
            Attribute.ErrorMessageResourceType is null &&
            localizer is not null)
        {
            var localizerKey = Attribute.ErrorMessageResourceName;

            // clear the attributes so format string does not take
            // them into account while formatting
            Attribute.ErrorMessageResourceType = null;
            Attribute.ErrorMessageResourceName = null;

            var resource = localizer.GetString(localizerKey);
            if (resource is not null && !resource.ResourceNotFound)
                Attribute.ErrorMessage = resource.Value;
        }

        var errorMessage = Attribute.FormatErrorMessage(MemberName);
        
        return Attribute.IsValid(propertyValue)
            ? ValidationResult.Success
            : new ValidationResult(errorMessage ?? string.Empty);
    }

    /// <inheritdoc />
    public ValueTask<ValidationResult?> ValidateAsync(object? instance, IStringLocalizer? localizer = null)
    {
        var result = Validate(instance, localizer);
        return ValueTask.FromResult(result);
    }
}
