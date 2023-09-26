using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace Unosquare.ModelValidation;


/// <summary>
/// A class that provides access to relevant data within the validation process.
/// </summary>
public class MemberValidatorContext
    : MemberValidatorContextBase
{
    private readonly MemberCustomValidator _validator;

    /// <summary>
    /// Creates a new instance of the <see cref="MemberValidatorContext"/>
    /// </summary>
    /// <param name="validator"></param>
    /// <param name="instance"></param>
    /// <param name="localizer"></param>
    public MemberValidatorContext(
        MemberCustomValidator validator,
        object instance,
        IStringLocalizer? localizer)
        : base(validator, instance, localizer)
    {
        _validator = validator;
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="MemberValidatorContextBase.Member"/> can
    /// be written to.
    /// </summary>
    public bool CanWrite => _validator.PropertySetter is not null;

    /// <summary>
    /// Gets the original value of the <see cref="MemberValidatorContextBase.Member"/>.
    /// </summary>
    /// <returns>The value reed.</returns>
    public object? GetValue() => _validator.PropertyGetter?.Invoke(Instance);

    /// <summary>
    /// Tries to read the value from the <see cref="MemberValidatorContextBase.Member"/>.
    /// </summary>
    /// <param name="value">The return value.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool TryGetValue(out object? value)
    {
#pragma warning disable CA1031
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
#pragma warning restore CA1031
    }

    /// <summary>
    /// Writes the specified value to the <see cref="MemberValidatorContextBase.Member"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be written.</exception>
    public void SetValue(object? value)
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
    public bool TrySetValue(object? value)
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

