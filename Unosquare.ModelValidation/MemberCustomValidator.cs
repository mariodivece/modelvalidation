using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Unosquare.ModelValidation;

/// <summary>
/// Represens an <see cref="IMemberValidator"/> that uses custom logic
/// to validate a member.
/// </summary>
public class MemberCustomValidator
    : IMemberValidator
{
    private Func<MemberValidatorContext, object?, ValueTask<object?>>? _preValidationLogic;
    private Func<MemberValidatorContext, object?, ValueTask<ValidationResult?>>? _validationLogic;
    private Func<MemberValidatorContext, object?, ValueTask>? _postValidationLogic;

    internal MemberCustomValidator(PropertyInfo property,
        Func<object, object?>? propertyGetter = null,
        Action<object, object?>? propertySetter = null)
    {
        ModelType = property.DeclaringType ?? typeof(object);
        Member = property;
        MemberName = property.Name;
        PropertyGetter = propertyGetter;
        PropertySetter = propertySetter;
    }

    /// <inheritdoc />
    public Type ModelType { get; }

    /// <inheritdoc />
    public string MemberName { get; }

    /// <inheritdoc />
    public PropertyInfo Member { get; }

    /// <summary>
    /// Gets a function to read the member's value.
    /// </summary>
    public Func<object, object?>? PropertyGetter { get; }

    /// <summary>
    /// Gets a method to write the member's value.
    /// </summary>
    public Action<object, object?>? PropertySetter { get; }

    /// <summary>
    /// Executes logic that retrieves the current value of the target member and
    /// formats it (without setting it) so that the value is apssed on the the validation
    /// logic.
    /// </summary>
    /// <param name="preValidation">The function that formats the current value.</param>
    /// <returns>This isntance for fluent API support.</returns>
    public MemberCustomValidator WithPreValidationAsync(Func<MemberValidatorContext, object?, ValueTask<object?>> preValidation)
    {
        _preValidationLogic = preValidation;
        return this;
    }

    /// <summary>
    /// Executes logic that retrieves the current value of the target member and
    /// formats it (without setting it) so that the value is apssed on the the validation
    /// logic.
    /// </summary>
    /// <param name="preValidation">The function that formats the current value.</param>
    /// <returns>This isntance for fluent API support.</returns>
    public MemberCustomValidator WithPreValidation(Func<MemberValidatorContext, object?, object?> preValidation)
    {
        _preValidationLogic = (context, originalValue) => ValueTask.FromResult(preValidation.Invoke(context, originalValue));
        return this;
    }

    /// <summary>
    /// Configures logic that retrieves the current value (input filtered or as-read) and determines
    /// if the value passes the validation. Please use the <see cref="MemberValidatorContextBase.Pass"/> or
    /// <see cref="MemberValidatorContextBase.Fail"/> methods to return validation results.
    /// The function passed is the second in the validation chain.
    /// </summary>
    /// <param name="validation">The function implementing validation logic.</param>
    /// <returns>This isntance for fluent API support.</returns>
    public MemberCustomValidator WithValidation(Func<MemberValidatorContext, object?, ValueTask<ValidationResult?>> validation)
    {
        _validationLogic = validation;
        return this;
    }

    /// <summary>
    /// Configures logic that is executed when the validation logic completes.
    /// Check if the validation logic succeeds by reading the <see cref="MemberValidatorContextBase.IsFailed"/>.
    /// Typically you will want to call the <see cref="MemberValidatorContext.TrySetValue"/> method when
    /// the validation succeeds and pre-validation logic provides a formatted value.
    /// property.
    /// </summary>
    /// <param name="postValidation">The logic to be executed when validation completes.</param>
    /// <returns>This isntance for fluent API support.</returns>
    public MemberCustomValidator WithPostValidationAsync(Func<MemberValidatorContext, object?, ValueTask> postValidation)
    {
        _postValidationLogic = postValidation;
        return this;
    }

    /// <summary>
    /// Configures logic that is executed when the validation logic completes.
    /// Check if the validation logic succeeds by reading the <see cref="MemberValidatorContextBase.IsFailed"/>.
    /// Typically you will want to call the <see cref="MemberValidatorContext.TrySetValue"/> method when
    /// the validation succeeds and pre-validation logic provides a formatted value.
    /// property.
    /// </summary>
    /// <param name="postValidation">The logic to be executed when validation completes.</param>
    /// <returns>This isntance for fluent API support.</returns>
    public MemberCustomValidator WithPostValidation(Action<MemberValidatorContext, object?> postValidation)
    {
        _postValidationLogic = (context, currentValue) =>
        {
            postValidation.Invoke(context, currentValue);
            return ValueTask.CompletedTask;
        };

        return this;
    }

    /// <inheritdoc />
    public virtual ValidationResult? Validate(object? instance, IStringLocalizer? localizer = null) =>
        ValidateAsync(instance, localizer).AsTask().GetAwaiter().GetResult();

    /// <inheritdoc />
    public virtual async ValueTask<ValidationResult?> ValidateAsync(object? instance, IStringLocalizer? localizer = null)
    {
        if (instance is null || !instance.GetType().IsAssignableTo(ModelType))
            throw new ArgumentException($"Instance must not be null and of type '{ModelType.FullName}'.", nameof(instance));

        if (PropertyGetter is null)
            throw new InvalidOperationException($"{nameof(PropertyGetter)} must be set to continue validation.");

        var result = ValidationResult.Success;
        var context = new MemberValidatorContext(this, instance, localizer);

        // obtain the base value
        var value = PropertyGetter.Invoke(instance);

        // run the input filter if available
        if (_preValidationLogic is not null)
            value = await _preValidationLogic.Invoke(context, value).ConfigureAwait(false);


        // now, run the validation action that produces a result
        if (_validationLogic is not null)
            result = await _validationLogic.Invoke(context, value).ConfigureAwait(false);

        // run the completion logic
        if (_postValidationLogic is not null)
            await _postValidationLogic.Invoke(context, value).ConfigureAwait(false);

        return result;
    }
}

