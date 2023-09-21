using System.ComponentModel.DataAnnotations;

namespace Unosquare.ModelValidation;

public sealed record ModelValidationResult
{
    private static readonly Dictionary<string, ValidationResult> EmptyResults = new(0);

    private readonly IDictionary<string, ValidationResult> _validationResults;
    private readonly Lazy<string[]> _fieldNames;
  
    internal ModelValidationResult(IDictionary<string, ValidationResult>? validationResults)
    {
        _validationResults = validationResults ?? EmptyResults;
        _fieldNames = new(() => _validationResults.Keys.ToArray(), false);
        ErrorCount = _validationResults.Count(kvp => kvp.Value is not null);
    }

    public static ModelValidationResult Empty { get; } = new(EmptyResults);

    public ValidationResult? this[string fieldName] => ForField(fieldName);

    public IReadOnlyList<string> FieldNames => _fieldNames.Value;

    public int ErrorCount { get; }

    public bool IsValid => ErrorCount <= 0;

    public ValidationResult? ForField(string fieldName) => _validationResults.TryGetValue(fieldName, out var result)
        ? result
        : ValidationResult.Success;

}
