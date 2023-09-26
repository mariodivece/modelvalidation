using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Unosquare.ModelValidation;

/// <summary>
/// Provides extension methods to easily add pre-built member validators to model validators.
/// </summary>
public static class ValidationExtensions
{
    private static readonly EmailAddressAttribute DataValidatorEmailAddress = new();

    /// <summary>
    /// Adds an email validation attribute for the specified member expression.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="validator">The model validator to add the field validation to.</param>
    /// <param name="memberLambda">The property expression.</param>
    /// <param name="errorMessage">The default error message.</param>
    /// <param name="localizerKey">An option lookup key for the localizer.</param>
    /// <returns>The same instance fo the model validator.</returns>
    public static ModelValidator<TModel> AddEmail<TModel>(
        this ModelValidator<TModel> validator, Expression<Func<TModel, string?>> memberLambda,
        string? errorMessage = null,
        string? localizerKey = null)
    {
        if (validator is null)
            throw new ArgumentNullException(nameof(validator));

        _ = validator.AddCustom(memberLambda, config =>
        {
            _ = config.WithPreValidation((context, originalValue) =>
             {
                 if (originalValue is null || originalValue is not string stringValue)
                     return originalValue;

#pragma warning disable CA1308 // Normalize strings to uppercase
                 return stringValue?.Trim()
                    .ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
             })
            .WithValidation((context, filteredValue) =>
            {
                if (filteredValue is null)
                    return context.Pass();

                if (!DataValidatorEmailAddress.IsValid(filteredValue))
                    return context.Fail(context.Localize(localizerKey, errorMessage ?? "Invalid email format."));

                return context.Pass();
            })
            .WithPostValidation((context, filteredValue) =>
            {
                if (context.IsFailed)
                    return;

                _ = context.TrySetValue(filteredValue);
            });
        });

        return validator;
    }

    /// <summary>
    /// Adds a required validation attribute for the specified member expression.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TMember">The property type.</typeparam>
    /// <param name="validator">The model validator to add the field validation to.</param>
    /// <param name="memberLambda">The property expression.</param>
    /// <param name="errorMessage">The default error message.</param>
    /// <param name="localizerKey">An option lookup key for the localizer.</param>
    /// <returns>The same instance fo the model validator.</returns>
    public static ModelValidator<TModel> AddRequired<TModel, TMember>(
        this ModelValidator<TModel> validator, Expression<Func<TModel, TMember?>> memberLambda,
        string? errorMessage = null,
        string? localizerKey = null)
    {
        if (validator is null)
            throw new ArgumentNullException(nameof(validator));

        _ = validator.AddAttribute(memberLambda, () =>
        {
            return new RequiredAttribute()
            {
                AllowEmptyStrings = false,
                ErrorMessageResourceName = localizerKey,
                ErrorMessage = errorMessage ?? "Field is required."
            };
        });

        return validator;
    }
}
