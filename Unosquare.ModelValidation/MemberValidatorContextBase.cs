using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace Unosquare.ModelValidation;

/// <summary>
/// Provides a base implementation for member validator context.
/// </summary>
public class MemberValidatorContextBase
{
    /// <summary>
    /// Creates a new instance of the <see cref="MemberValidatorContextBase"/> class.
    /// </summary>
    /// <param name="validator">The underlying validator.</param>
    /// <param name="instance">The model instance.</param>
    /// <param name="localizer">The localizer.</param>
    protected MemberValidatorContextBase(
        IMemberValidator validator,
        object instance,
        IStringLocalizer? localizer)
    {
        Validator = validator;
        Instance = instance;
        Localizer = localizer;
    }

    /// <summary>
    /// Gets the source validator.
    /// </summary>
    protected IMemberValidator Validator { get; }

    /// <summary>
    /// Gets the instance for the model being validated.
    /// </summary>
    public object Instance { get; }

    /// <summary>
    /// Gets the name of the member.
    /// </summary>
    public string MemberName => Validator.MemberName;

    /// <summary>
    /// Gets the associated property information.
    /// </summary>
    public PropertyInfo Member => Validator.Member;

    /// <summary>
    /// Gets the current string localizer for error message reporting.
    /// </summary>
    public IStringLocalizer? Localizer { get; }

    /// <summary>
    /// Gets the current validation result as set by the <see cref="Pass"/>
    /// or <see cref="Fail"/> methods.
    /// </summary>
    public virtual ValidationResult? ValidationResult { get; private set; }

    /// <summary>
    /// Gets a value indicating whether validation has been performed and
    /// the validation did not succeed.
    /// </summary>
    public virtual bool IsFailed => ValidationResult is not null;

    /// <summary>
    /// Signals that the state of this context has failed the validation.
    /// </summary>
    /// <param name="messages">Any user messages as set by the field validator.</param>
    /// <returns>A validation result.</returns>
    public ValueTask<ValidationResult?> Fail(params string[] messages)
    {
        ValidationResult = new ValidationResult(string.Join(Environment.NewLine, messages));
        return ValueTask.FromResult<ValidationResult?>(ValidationResult);
    }

    /// <summary>
    /// Signals that the state of this context has passed the validation.
    /// </summary>
    /// <returns>A validation result that will be null -- meaning successful validation.</returns>
    public ValueTask<ValidationResult?> Pass()
    {
        ValidationResult = ValidationResult.Success;
        return ValueTask.FromResult(ValidationResult);
    }


    /// <summary>
    /// Localizes a string given a key.
    /// </summary>
    /// <param name="key">The key to look for in the passed <see cref="IStringLocalizer"/>.</param>
    /// <param name="defaultText">The text to display whenever the key is not found in the localizer.</param>
    /// <param name="arguments">Optional string formatter arguments.</param>
    /// <returns></returns>
    public string Localize(string? key, string? defaultText, params object[] arguments)
    {
        if (Localizer is null || key is null)
            return string.Format(CultureInfo.CurrentCulture, defaultText ?? string.Empty, arguments);

        var resource = Localizer[key];

        return resource is null || resource.ResourceNotFound || resource.Value is null
            ? string.Format(CultureInfo.CurrentCulture, defaultText ?? string.Empty, arguments)
            : string.Format(CultureInfo.CurrentCulture, resource.Value, arguments);
    }
}
