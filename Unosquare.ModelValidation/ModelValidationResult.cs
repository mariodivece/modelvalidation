using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Unosquare.ModelValidation;

/// <summary>
/// Represeents a set of validation results for a model.
/// </summary>
public record ModelValidationResult
{
    private static readonly IDictionary<string, IReadOnlyList<ValidationResult>> EmptyResults =
        new Dictionary<string, IReadOnlyList<ValidationResult>>(0);

    private readonly IDictionary<string, IReadOnlyList<ValidationResult>> _validationResults;
    private readonly Lazy<string[]> _fieldNames;
  
    internal ModelValidationResult(IDictionary<string, IReadOnlyList<ValidationResult>>? validationResults)
    {
        _validationResults = validationResults ?? EmptyResults;
        _fieldNames = new(() => _validationResults.Keys.ToArray(), false);
        ErrorCount = _validationResults.Sum(c => c.Value.Count);
    }

    /// <summary>
    /// Gets a representation of empty validation results.
    /// </summary>
    public static ModelValidationResult Empty { get; } = new(EmptyResults);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memberName"></param>
    /// <returns></returns>
    public IReadOnlyList<ValidationResult> this[string memberName] => For(memberName);

    /// <summary>
    /// Gets a set of member names that contain errors.
    /// </summary>
    public IReadOnlyList<string> MemberNames => _fieldNames.Value;

    /// <summary>
    /// Gets a value indicating the number of errors that were found.
    /// </summary>
    public int ErrorCount { get; }

    /// <summary>
    /// Gets a value indicating whether no errors are present in the model validation.
    /// </summary>
    public bool IsValid => ErrorCount <= 0;

    /// <summary>
    /// Retrieves validation results for the specified member.
    /// </summary>
    /// <param name="memberName">The unique name of the member.</param>
    /// <returns>A list of validation results. Will return an empty set if all succeeded.</returns>
    public IReadOnlyList<ValidationResult> For(string memberName) => _validationResults.TryGetValue(memberName, out var result)
        ? result
        : Array.Empty<ValidationResult>();

    /// <summary>
    /// Manually adds a validation result for a given member.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <param name="validationResult">The validation result to add.</param>
    /// <returns>The instance for fluent API support.</returns>
    public ModelValidationResult Add(string memberName, ValidationResult? validationResult)
    {
        if (string.IsNullOrWhiteSpace(memberName))
            return this;

        if (validationResult == ValidationResult.Success || validationResult is null)
            return this;

        if (!_validationResults.TryGetValue(memberName, out var result))
        {
            result = new List<ValidationResult>();
            _validationResults[memberName] = result;
        }

        (result as List<ValidationResult>)!.Add(validationResult);
        return this;
    }

    /// <summary>
    /// Manually adds a validation result for a given member.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <param name="errorMessage">The validation error message to add.</param>
    /// <returns>The instance for fluent API support.</returns>
    public ModelValidationResult Add(string memberName, string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(memberName))
            return this;

        if (string.IsNullOrWhiteSpace(errorMessage))
            return this;

        return Add(memberName, new ValidationResult(errorMessage));
    }

    /// <summary>
    /// Determines if a member has validation errors.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <returns>True if invalid. False otherwise.</returns>
    public bool IsInvalid(string memberName) => For(memberName).Count > 0;

    /// <summary>
    /// Retrieves the first error message (if any) found for the given member name.
    /// </summary>
    /// <param name="memberName">The name of the member.</param>
    /// <returns>The string containing the rror message.</returns>
    public string? ErrorMessage(string memberName) => memberName is null
        ? default
        : For(memberName)?.FirstOrDefault()?.ErrorMessage;

    /// <summary>
    /// Converts the current validation result to a stringly-typed model validation result.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <returns>The newly created model validation result.</returns>
    public ModelValidationResult<TModel> ToTyped<TModel>() => new(this);
}

/// <summary>
/// Represents a strongly-typed version of <see cref="ModelValidationResult"/>
/// </summary>
/// <typeparam name="TModel">The model type.</typeparam>
public record ModelValidationResult<TModel>
    : ModelValidationResult
{
    internal ModelValidationResult(ModelValidationResult original)
        : base(original)
    {
        // placeholder
    }

    /// <summary>
    /// Manually adds a validation result for a given member.
    /// </summary>
    /// <param name="expression">The member expression.</param>
    /// <param name="validationResult">The validation result to add.</param>
    /// <returns>The instance for fluent API support.</returns>
    public ModelValidationResult<TModel> Add<TMember>(Expression<Func<TModel, TMember>> expression, ValidationResult? validationResult)
    {
        if (expression is null)
            return this;

        if (validationResult == ValidationResult.Success || validationResult is null)
            return this;

        if (!TryGetProperty(expression, out var property))
            return this;

        Add(property.Name, validationResult);

        return this;
    }

    /// <summary>
    /// Manually adds a validation result for a given member.
    /// </summary>
    /// <param name="expression">The member expression.</param>
    /// <param name="errorMessage">The validation error message to add.</param>
    /// <returns>The instance for fluent API support.</returns>
    public ModelValidationResult<TModel> Add<TMember>(Expression<Func<TModel, TMember>> expression, string? errorMessage)
    {
        if (expression is null)
            return this;

        if (string.IsNullOrWhiteSpace(errorMessage))
            return this;

        if (!TryGetProperty(expression, out var property))
            return this;

        Add(property.Name, new ValidationResult(errorMessage));

        return this;
    }

    /// <summary>
    /// Determines if a member has validation errors.
    /// </summary>
    /// <param name="expression">The member expression.</param>
    /// <returns>True if invalid. False otherwise.</returns>
    public bool IsInvalid<TMember>(Expression<Func<TModel, TMember>> expression)
    {
        if (expression is null)
            return false;

        if (!TryGetProperty(expression, out var property))
            return false;

        return IsInvalid(property.Name);
    }

    /// <summary>
    /// Retrieves the first error message (if any) found for the given member name.
    /// </summary>
    /// <param name="expression">The name of the member.</param>
    /// <returns>The string containing the rror message.</returns>
    public string? ErrorMessage<TMember>(Expression<Func<TModel, TMember>> expression)
    {
        if (expression is null)
            return default;

        if (!TryGetProperty(expression, out var property))
            return default;

        return ErrorMessage(property.Name);
    }

    private static bool TryGetProperty<TMember>(
        Expression<Func<TModel, TMember>> expression,
        [MaybeNullWhen(false)] out PropertyInfo property)
    {
        property = null;

        if (expression is null ||
            expression.Body is not MemberExpression memberExpression ||
            memberExpression.Member is not PropertyInfo outputProperty ||
            !outputProperty.CanRead)
        {
            return false;
        }

        property = outputProperty;
        return true;
    }
}
