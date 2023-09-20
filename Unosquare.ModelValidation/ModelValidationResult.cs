namespace Unosquare.ModelValidation;

public sealed record ModelValidationResult
{
    private static readonly Dictionary<string, FieldValidationResult> EmptyResults = new(0);

    private readonly IDictionary<string, FieldValidationResult> _validationResults;
    private readonly Lazy<string[]> _fieldNames;
  
    internal ModelValidationResult(IDictionary<string, FieldValidationResult>? validationResults)
    {
        _validationResults = validationResults ?? EmptyResults;
        _fieldNames = new(() => _validationResults.Keys.ToArray(), false);
        ErrorCount = _validationResults.Count(kvp => kvp.Value.IsValid == false);
    }

    public static ModelValidationResult Empty { get; } = new(EmptyResults);

    public FieldValidationResult this[string fieldName] => ForField(fieldName);

    public IReadOnlyList<string> FieldNames => _fieldNames.Value;

    public int ErrorCount { get; }

    public bool IsValid => ErrorCount <= 0;

    public FieldValidationResult ForField(string fieldName) => _validationResults.TryGetValue(fieldName, out var result)
        ? result
        : FieldValidationResult.Success;

}
