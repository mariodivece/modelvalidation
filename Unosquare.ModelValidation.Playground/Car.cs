using System.ComponentModel.DataAnnotations;

namespace Unosquare.ModelValidation.Playground;

public record Car
{
    [Range(1, 10, ErrorMessage = "Unfortunately, this field '{0}' must have a value between {1} and {2}.")]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }
}