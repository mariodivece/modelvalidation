namespace Unosquare.ModelValidation;

/// <summary>
/// Represents a model validator that can be instantiated given a model type.
/// </summary>
public class ModelValidator
    : ModelValidatorBase<ModelValidator>
{
    /// <summary>
    /// Creates a new instance of the <see cref="ModelValidator"/> class.
    /// </summary>
    /// <param name="modelType">The model type.</param>
    public ModelValidator(Type modelType)
        : base(modelType)
    {
    }
}
