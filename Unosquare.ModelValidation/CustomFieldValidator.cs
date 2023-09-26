using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Unosquare.ModelValidation;

public class CustomFieldValidator
    : IFieldValidator
{
    private Func<FieldValidatorContext, object?, ValueTask<object?>>? _inputFilter;
    private Func<FieldValidatorContext, object?, ValueTask<ValidationResult?>>? _validator;
    private Func<FieldValidatorContext, object?, ValueTask>? _postSuccess;

    internal CustomFieldValidator(PropertyInfo property,
        Func<object, object?>? propertyGetter = null,
        Action<object, object?>? propertySetter = null)
    {
        ModelType = property.DeclaringType ?? typeof(object);
        Property = property;
        FieldName = property.Name;
        PropertyGetter = propertyGetter;
        PropertySetter = propertySetter;
    }

    public Type ModelType { get; }

    public string FieldName { get; }

    public PropertyInfo Property { get; }

    public Func<object, object?>? PropertyGetter { get; }

    public Action<object, object?>? PropertySetter { get; }

    public CustomFieldValidator WithInputFilterAsync(Func<FieldValidatorContext, object?, ValueTask<object?>> inputFilter)
    {
        _inputFilter = inputFilter;
        return this;
    }

    public CustomFieldValidator WithInputFilter(Func<FieldValidatorContext, object?, object?> inputFilter)
    {
        _inputFilter = (context, originalValue) => ValueTask.FromResult(inputFilter.Invoke(context, originalValue));
        return this;
    }

    public CustomFieldValidator WithValidation(Func<FieldValidatorContext, object?, ValueTask<ValidationResult?>> validator)
    {
        _validator = validator;
        return this;
    }

    public CustomFieldValidator WithPostSuccessAsync(Func<FieldValidatorContext, object?, ValueTask> postSuccess)
    {
        _postSuccess = postSuccess;
        return this;
    }

    public CustomFieldValidator WithPostSuccess(Action<FieldValidatorContext, object?> postSuccess)
    {
        _postSuccess = (context, currentValue) =>
        {
            postSuccess.Invoke(context, currentValue);
            return ValueTask.CompletedTask;
        };

        return this;
    }

    public virtual ValidationResult? Validate(object? instance, IStringLocalizer? localizer = null) =>
        ValidateAsync(instance, localizer).AsTask().GetAwaiter().GetResult();

    public virtual async ValueTask<ValidationResult?> ValidateAsync(object? instance, IStringLocalizer? localizer = null)
    {

        if (instance is null || !instance.GetType().IsAssignableTo(ModelType))
            throw new ArgumentException($"Instance must not be null and of type '{ModelType.FullName}'.", nameof(instance));

        if (PropertyGetter is null)
            throw new InvalidOperationException($"{nameof(PropertyGetter)} must be set to continue validation.");

        var result = ValidationResult.Success;
        var context = new FieldValidatorContext(this, instance, localizer);

        // obtain the base value
        var value = PropertyGetter.Invoke(instance);

        // run the input filter if available
        if (_inputFilter is not null)
            value = await _inputFilter.Invoke(context, value).ConfigureAwait(false);

        // check if we have a validator. if we don't,
        // it means everything passes but we may still
        // need to run a post-processing action
        if (_validator is null)
        {
            // run the post-success action if available
            if (_postSuccess is not null)
                await _postSuccess.Invoke(context, value).ConfigureAwait(false);

            return result;
        }

        // now, run the validation action that produces a result
        result = await _validator.Invoke(context, value).ConfigureAwait(false);

        // check if result is success
        if (result is null)
        {
            if (_postSuccess is not null)
                await _postSuccess.Invoke(context, value).ConfigureAwait(false);
        }

        return result;
    }
}

public class CustomFieldValidator<TModel, TMember>
    : CustomFieldValidator
{
    private PreValidationFormatAsync<TMember?, TModel>? _inputFilter;
    private ValidationMethodAsync<TModel, TMember?>? _validator;
    private PostValidationActionAsync<TModel, TMember?>? _postSuccess;

    internal CustomFieldValidator(
        PropertyInfo propertyInfo,
        Func<TModel, TMember> propertyGetter,
        Action<TModel, TMember?>? propertySetter = null)
        : base(propertyInfo, null, null)
    {
        PropertyGetter = propertyGetter;
        PropertySetter = propertySetter;
        ModelType = typeof(TModel);
    }

    public new Type ModelType { get; }

    public new Func<TModel, TMember?> PropertyGetter { get; }

    public new Action<TModel, TMember?>? PropertySetter { get; }

    public CustomFieldValidator<TModel, TMember> WithInputFilterAsync(PreValidationFormatAsync<TMember?, TModel> inputFilter)
    {
        _inputFilter = inputFilter;
        return this;
    }

    public CustomFieldValidator<TModel, TMember> WithInputFilter(PreValidationFormat<TMember?, TModel> inputFilter)
    {
        _inputFilter = (context, originalValue) => ValueTask.FromResult(inputFilter.Invoke(context, originalValue));
        return this;
    }

    public CustomFieldValidator<TModel, TMember> WithValidation(ValidationMethodAsync<TModel, TMember?> validator)
    {
        _validator = validator;
        return this;
    }

    public CustomFieldValidator<TModel, TMember> WithPostSuccessAsync(PostValidationActionAsync<TModel, TMember?> postSuccess)
    {
        _postSuccess = postSuccess;
        return this;
    }

    public CustomFieldValidator<TModel, TMember> WithPostSuccess(PostValidationAction<TModel, TMember?> postSuccess)
    {
        _postSuccess = (context, currentValue) =>
        {
            postSuccess.Invoke(context, currentValue);
            return ValueTask.CompletedTask;
        };

        return this;
    }

    public override ValidationResult? Validate(object? instance, IStringLocalizer? localizer = null) =>
        ValidateAsync(instance, localizer).AsTask().GetAwaiter().GetResult();

    public override async ValueTask<ValidationResult?> ValidateAsync(object? instance, IStringLocalizer? localizer = null)
    {
        if (instance is not TModel model)
            throw new ArgumentException($"Instance must not be null and of type '{typeof(TModel).FullName}'.", nameof(instance));

        var result = ValidationResult.Success;
        var context = new FieldValidatorContext<TModel, TMember?>(this, model, localizer);

        // obtain the base value
        TMember? value = PropertyGetter.Invoke(model);

        // run the input filter if available
        if (_inputFilter is not null)
            value = await _inputFilter.Invoke(context, value).ConfigureAwait(false);

        // check if we have a validator. if we don't,
        // it means everything passes but we may still
        // need to run a post-processing action
        if (_validator is null)
        {
            // run the post-success action if available
            if (_postSuccess is not null)
                await _postSuccess.Invoke(context, value).ConfigureAwait(false);

            return result;
        }

        // now, run the validation action that produces a result
        result = await _validator.Invoke(context, value).ConfigureAwait(false);

        // check if result is success
        if (result is null)
        {
            if (_postSuccess is not null)
                await _postSuccess.Invoke(context, value).ConfigureAwait(false);
        }

        return result;
    }
}
