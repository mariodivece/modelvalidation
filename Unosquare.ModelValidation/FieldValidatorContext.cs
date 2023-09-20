using Microsoft.Extensions.Localization;

namespace Unosquare.ModelValidation;

public sealed class FieldValidatorContext<TModel, TMember>
{
    private readonly FieldValidator<TModel, TMember> _validator;

    internal FieldValidatorContext(FieldValidator<TModel, TMember> validator, TModel instance, IStringLocalizer? localizer)
    {
        _validator = validator;
        Instance = instance;
        Localizer = localizer;
    }

    public TModel Instance { get; }

    public string FieldName => _validator.FieldName;

    public IStringLocalizer? Localizer { get; }

    public bool CanWrite => _validator.PropertySetter is not null;

    public TMember GetValue() => _validator.PropertyGetter.Invoke(Instance);

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

    public void SetValue(TMember value)
    {
        if (!CanWrite)
            throw new InvalidOperationException($"Member '{FieldName}' cannot be written to.");

        _validator.PropertySetter!.Invoke(Instance, value);
    }

    public bool TrySetValue(TMember value)
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

    public ValueTask<FieldValidationResult> Fail(int errorCode = -1, params string[] messages) =>
        ValueTask.FromResult(new FieldValidationResult()
        {
            IsValid = false,
            ResultCode = errorCode,
            ErrorMessages = messages
        });

    public ValueTask<FieldValidationResult> Fail(string message, int errorCode = -1) =>
        ValueTask.FromResult(new FieldValidationResult()
        {
            IsValid = false,
            ResultCode = errorCode,
            ErrorMessages = new[] { message }
        });

    public ValueTask<FieldValidationResult> Pass() =>
        ValueTask.FromResult(FieldValidationResult.Success);
}
