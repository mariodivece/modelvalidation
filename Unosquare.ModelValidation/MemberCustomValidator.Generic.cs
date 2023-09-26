using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Unosquare.ModelValidation;

/// <summary>
/// A generic, strongly-typed variation of the <see cref="MemberCustomValidator"/> class.
/// </summary>
/// <typeparam name="TModel">The owning model type.</typeparam>
/// <typeparam name="TMember">The member type.</typeparam>
public class MemberCustomValidator<TModel, TMember>
    : IMemberValidator
{
    private PreValidationValueAsync<TMember?, TModel>? _preValidationLogic;
    private ValidationMethodAsync<TModel, TMember?>? _validationLogic;
    private PostValidationActionAsync<TModel, TMember?>? _postValidationLogic;

    internal MemberCustomValidator(
        PropertyInfo propertyInfo,
        Func<TModel, TMember> propertyGetter,
        Action<TModel, TMember?>? propertySetter = null)
    {
        PropertyGetter = propertyGetter;
        PropertySetter = propertySetter;
        ModelType = typeof(TModel);
        Member = propertyInfo;
    }

    /// <inheritdoc />
    public Type ModelType { get; }

    /// <inheritdoc />
    public PropertyInfo Member { get; }

    /// <inheritdoc />
    public string MemberName => Member.Name;

    /// <summary>
    /// Gets a lambda that retrieves the value of the target member.
    /// </summary>
    public Func<TModel, TMember?> PropertyGetter { get; }

    /// <summary>
    /// Gets a lambda that sets the value of the target member.
    /// </summary>
    public Action<TModel, TMember?>? PropertySetter { get; }

    /// <summary>
    /// Executes logic that retrieves the current value of the target member and
    /// formats it (without setting it) so that the value is apssed on the the validation
    /// logic.
    /// </summary>
    /// <param name="preValidation">The function that formats the current value.</param>
    /// <returns>This isntance for fluent API support.</returns>
    public MemberCustomValidator<TModel, TMember> WithPreValidationAsync(PreValidationValueAsync<TMember?, TModel> preValidation)
    {
        _preValidationLogic = preValidation;
        return this;
    }

    /// <summary>
    /// Configures logic that retrieves the current value of the target member and
    /// formats it (without setting it) so that the value is passed on the the validation
    /// logic. The function passed is the first in the validation chain.
    /// </summary>
    /// <param name="preValidation">The function that formats the current value.</param>
    /// <returns>This isntance for fluent API support.</returns>
    public MemberCustomValidator<TModel, TMember> WithPreValidation(PreValidationValue<TMember?, TModel> preValidation)
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
    public MemberCustomValidator<TModel, TMember> WithValidation(ValidationMethodAsync<TModel, TMember?> validation)
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
    public MemberCustomValidator<TModel, TMember> WithPostValidationAsync(PostValidationActionAsync<TModel, TMember?> postValidation)
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
    public MemberCustomValidator<TModel, TMember> WithPostValidation(PostValidationAction<TModel, TMember?> postValidation)
    {
        _postValidationLogic = (context, currentValue) =>
        {
            postValidation.Invoke(context, currentValue);
            return ValueTask.CompletedTask;
        };

        return this;
    }

    /// <inheritdoc />
    public ValidationResult? Validate(object? instance, IStringLocalizer? localizer = null) =>
        ValidateAsync(instance, localizer).AsTask().GetAwaiter().GetResult();

    /// <inheritdoc />
    public async ValueTask<ValidationResult?> ValidateAsync(object? instance, IStringLocalizer? localizer = null)
    {
        if (instance is not TModel model)
            throw new ArgumentException($"Instance must not be null and of type '{typeof(TModel).FullName}'.", nameof(instance));

        var result = ValidationResult.Success;
        var context = new MemberValidatorContext<TModel, TMember?>(this, model, localizer);

        // obtain the base value
        var value = PropertyGetter.Invoke(model);

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

