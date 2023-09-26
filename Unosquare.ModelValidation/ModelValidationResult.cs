using System.ComponentModel.DataAnnotations;

namespace Unosquare.ModelValidation;

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

    public static ModelValidationResult Empty { get; } = new(EmptyResults);

    public IReadOnlyList<ValidationResult> this[string fieldName] => ForField(fieldName);

    public IReadOnlyList<string> FieldNames => _fieldNames.Value;

    public int ErrorCount { get; }

    public bool IsValid => ErrorCount <= 0;

    public IReadOnlyList<ValidationResult> ForField(string fieldName) => _validationResults.TryGetValue(fieldName, out var result)
        ? result
        : Array.Empty<ValidationResult>();
}
