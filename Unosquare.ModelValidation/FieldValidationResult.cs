namespace Unosquare.ModelValidation;

public sealed record FieldValidationResult
{
    public static FieldValidationResult Success { get; } = new()
    {
        ErrorMessages = Array.Empty<string>(),
        ResultCode = default,
        IsValid = true
    };

    public int ResultCode { get; init; } = 0;

    public bool IsValid { get; init; } = true;

    public IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();

    public override string ToString()
    {
        return string.Join(Environment.NewLine, ErrorMessages);
    }
}