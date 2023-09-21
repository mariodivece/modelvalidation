using System.ComponentModel.DataAnnotations;

namespace Unosquare.ModelValidation.Playground;

internal record Car
{
    [Range(1, 10, ErrorMessage = "Value must be between 1 and 10")]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }
}