using System.ComponentModel.DataAnnotations;

namespace Unosquare.ModelValidation;

/// <summary>
/// Represeents a set of validation results for a model.
/// </summary>
public sealed record ModelValidationResult
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
}
