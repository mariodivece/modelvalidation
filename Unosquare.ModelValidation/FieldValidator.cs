using Microsoft.Extensions.Localization;
using System.Reflection;

namespace Unosquare.ModelValidation;

public class FieldValidator<TModel, TMember>
    : IFieldValidator
{
    private PreValidationFormatAsync<TMember, TModel>? _inputFilter;
    private ValidationActionAsync<TModel, TMember>? _validator;
    private PostValidationActionAsync<TModel, TMember>? _postSuccess;

    public FieldValidator(
        PropertyInfo propertyInfo,
        Func<TModel, TMember> propertyGetter,
        Action<TModel, TMember>? propertySetter = null)
    {
        Property = propertyInfo;
        PropertyGetter = propertyGetter;
        PropertySetter = propertySetter;
    }

    public PropertyInfo Property { get; }

    public string FieldName => Property.Name;

    public Func<TModel, TMember> PropertyGetter { get; }

    public Action<TModel, TMember>? PropertySetter { get; }

    public FieldValidator<TModel, TMember> WithInputFilterAsync(PreValidationFormatAsync<TMember, TModel> inputFilter)
    {
        _inputFilter = inputFilter;
        return this;
    }

    public FieldValidator<TModel, TMember> WithInputFilter(PreValidationFormat<TMember, TModel> inputFilter)
    {
        _inputFilter = (context, originalValue) => ValueTask.FromResult(inputFilter.Invoke(context, originalValue));
        return this;
    }

    public FieldValidator<TModel, TMember> WithValidation(ValidationActionAsync<TModel, TMember> validator)
    {
        _validator = validator;
        return this;
    }

    public FieldValidator<TModel, TMember> WithPostSuccessAsync(PostValidationActionAsync<TModel, TMember> postSuccess)
    {
        _postSuccess = postSuccess;
        return this;
    }

    public FieldValidator<TModel, TMember> WithPostSuccess(PostValidationAction<TModel, TMember> postSuccess)
    {
        _postSuccess = (context, currentValue) =>
        {
            postSuccess.Invoke(context, currentValue);
            return ValueTask.CompletedTask;
        };

        return this;
    }

    public FieldValidationResult Validate(object instance, IStringLocalizer? localizer = null) =>
        ValidateAsync(instance, localizer).AsTask().GetAwaiter().GetResult();

    public async ValueTask<FieldValidationResult> ValidateAsync(object instance, IStringLocalizer? localizer = null)
    {
        if (instance is not TModel model)
            throw new ArgumentException($"Instance must not be null and of type '{typeof(TModel).FullName}'.", nameof(instance));

        var result = FieldValidationResult.Success;
        var context = new FieldValidatorContext<TModel, TMember>(this, model, localizer);

        // obtain the base value
        TMember value = PropertyGetter.Invoke(model);

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

        if (result.IsValid)
        {
            if (_postSuccess is not null)
                await _postSuccess.Invoke(context, value).ConfigureAwait(false);
        }

        return result;
    }
}
