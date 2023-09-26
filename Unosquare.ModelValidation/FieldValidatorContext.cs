using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Unosquare.ModelValidation;

public class FieldValidatorContext
{
    private readonly CustomFieldValidator _validator;

    public FieldValidatorContext(
        CustomFieldValidator validator,
        object instance,
        IStringLocalizer? localizer)
    {
        _validator = validator;
        Instance = instance;
        Localizer = localizer;
    }

    public object Instance { get; }

    public string FieldName => _validator.FieldName;

    public PropertyInfo Property => _validator.Property;

    public IStringLocalizer? Localizer { get; }

    public bool CanWrite => _validator.PropertySetter is not null;

    public virtual ValidationResult? ValidationResult { get; private set; }

    public object? GetValue() => _validator.PropertyGetter?.Invoke(Instance);

    public bool TryGetValue(out object? value)
    {
        value = default;
        try
        {
            value = GetValue();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SetValue(object? value)
    {
        if (!CanWrite)
            throw new InvalidOperationException($"Member '{FieldName}' cannot be written to.");

        _validator.PropertySetter!.Invoke(Instance, value);
    }

    public bool TrySetValue(object? value)
    {
        try
        {
            SetValue(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string Localize(string key, string defaultText, params object[] arguments)
    {
        if (Localizer is null)
            return string.Format(defaultText, arguments);

        var resource = Localizer[key];

        return resource is null || resource.ResourceNotFound
            ? defaultText
            : Localizer.GetString(key, arguments);
    }

    public ValueTask<ValidationResult?> Fail(params string[] messages)
    {
        ValidationResult = new ValidationResult(string.Join(Environment.NewLine, messages));
        return ValueTask.FromResult<ValidationResult?>(ValidationResult);
    }

    public ValueTask<ValidationResult?> Pass()
    {
        ValidationResult = ValidationResult.Success;
        return ValueTask.FromResult(ValidationResult);
    }
}

public sealed class FieldValidatorContext<TModel, TMember>
    : FieldValidatorContext
{
    private readonly CustomFieldValidator<TModel, TMember> _validator;

    internal FieldValidatorContext(
        CustomFieldValidator<TModel, TMember> validator,
        TModel instance,
        IStringLocalizer? localizer)
        : base(validator, instance!, localizer)
    {
        _validator = validator;
        Instance = instance;
    }

    public new TModel Instance { get; }

    public new TMember? GetValue() => _validator.PropertyGetter.Invoke(Instance);

    public bool TryGetValue(out TMember? value)
    {
        value = default;
        try
        {
            value = GetValue();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SetValue(TMember? value)
    {
        if (!CanWrite)
            throw new InvalidOperationException($"Member '{FieldName}' cannot be written to.");

        _validator.PropertySetter!.Invoke(Instance, value);
    }

    public bool TrySetValue(TMember? value)
    {
        try
        {
            SetValue(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
