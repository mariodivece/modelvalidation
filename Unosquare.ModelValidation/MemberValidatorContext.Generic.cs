using Microsoft.Extensions.Localization;

namespace Unosquare.ModelValidation;

/// <summary>
/// A class that provides access to relevant data within the validation process.
/// </summary>
public sealed class MemberValidatorContext<TModel, TMember>
    : MemberValidatorContextBase
{
    private readonly MemberCustomValidator<TModel, TMember> _validator;

    /// <summary>
    /// Creates a new instance of the <see cref="MemberValidatorContext{TModel, TMember}"/> class.
    /// </summary>
    /// <param name="validator">The field validator.</param>
    /// <param name="instance">The model instance.</param>
    /// <param name="localizer">The string localizer.</param>
    public MemberValidatorContext(
        MemberCustomValidator<TModel, TMember> validator,
        TModel instance,
        IStringLocalizer? localizer)
        : base(validator, instance!, localizer)
    {
        _validator = validator;
        Instance = instance;
    }

    /// <summary>
    /// Gets the instance for the model being validated.
    /// </summary>
    public new TModel Instance { get; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="MemberValidatorContextBase.Member"/> can
    /// be written to.
    /// </summary>
    public bool CanWrite => _validator.PropertySetter is not null;

    /// <summary>
    /// Gets the original value of the <see cref="MemberValidatorContextBase.Member"/>.
    /// </summary>
    /// <returns>The value read.</returns>
    public TMember? GetValue() => _validator.PropertyGetter.Invoke(Instance);

    /// <summary>
    /// Tries to read the value from the <see cref="MemberValidatorContextBase.Member"/>.
    /// </summary>
    /// <param name="value">The return value.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool TryGetValue(out TMember? value)
    {
        value = default;
        try
        {
            value = GetValue();
            return true;
        }
#pragma warning disable CA1031
        catch
        {
            return false;
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Writes the specified value to the <see cref="MemberValidatorContextBase.Member"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be written.</exception>
    public void SetValue(TMember? value)
    {
        if (!CanWrite)
            throw new InvalidOperationException($"Member '{MemberName}' cannot be written to.");

        _validator.PropertySetter!.Invoke(Instance, value);
    }

    /// <summary>
    /// Tries to write a value to the <see cref="MemberValidatorContextBase.Member"/>.
    /// </summary>
    /// <param name="value">The value to be written.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool TrySetValue(TMember? value)
    {
        try
        {
            SetValue(value);
            return true;
        }
#pragma warning disable CA1031
        catch
        {
            return false;
        }
#pragma warning restore CA1031
    }
}
