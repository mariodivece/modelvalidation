using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Unosquare.ModelValidation;

/// <summary>
/// Represents a model validator for the given model tye.
/// </summary>
/// <typeparam name="TModel">The target model type.</typeparam>
public class ModelValidator<TModel>
    : ModelValidatorBase<ModelValidator<TModel>>
{
    private const string BadPropertyExpression = "The expression must refer to a public, instance and readable property.";

    /// <summary>
    /// Creates a new instance of the <see cref="ModelValidator{TModel}"/> class.
    /// </summary>
    public ModelValidator()
        : base(typeof(TModel))
    {
        // placeholder
    }

    /// <summary>
    /// Adds <see cref="MemberAttributeValidator"/> instances
    /// for a single property by providing the name of the property.
    /// </summary>
    /// <param name="memberLambda">The lambda expressionr referring to a property.</param>
    /// <returns>This instance for fluent API support.</returns>
    public ModelValidator<TModel> AddAttributes<TMember>(Expression<Func<TModel, TMember>> memberLambda)
    {
        if (!TryGetProperty(memberLambda, out var propertyInfo))
            throw new ArgumentException(BadPropertyExpression, nameof(memberLambda));

        return AddAttributes(propertyInfo);
    }

    /// <summary>
    /// Adds a validator based on a <see cref="ValidationAttribute"/> instance.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type.</typeparam>
    /// <param name="memberLambda">The lambda expressionr referring to a property.</param>
    /// <param name="attributeFactory">The function that creates an instance of the attribute.</param>
    /// <typeparam name="TMember">The type of the property.</typeparam>
    /// <returns>This instance for fluent API support.</returns>
    public ModelValidator<TModel> AddAttribute<TMember, TAttribute>(
        Expression<Func<TModel, TMember>> memberLambda,
        Func<TAttribute> attributeFactory)
        where TAttribute : ValidationAttribute
    {
        if (!TryGetProperty(memberLambda, out var propertyInfo))
            throw new ArgumentException(BadPropertyExpression, nameof(memberLambda));

        return AddAttribute(propertyInfo, attributeFactory);
    }

    /// <summary>
    /// Adds a custom field validator to the associated property.
    /// </summary>
    /// <typeparam name="TMember">The property type.</typeparam>
    /// <param name="memberLambda">The lambda expressionr referring to a property.</param>
    /// <param name="validatorConfig">The validator configuration method that contains the field validator.</param>
    /// <returns>This instance for fluent API support.</returns>
    public ModelValidator<TModel> AddCustom<TMember>(
        Expression<Func<TModel, TMember>> memberLambda,
        Action<MemberCustomValidator<TModel, TMember>> validatorConfig)
    {
        if (!TryGetProperty(memberLambda, out var propertyInfo))
            throw new ArgumentException(BadPropertyExpression, nameof(memberLambda));

        // Work on the property getters and setters
        var propertyGetter = GetPropertyGetter<TMember>(propertyInfo);
        var propertySetter = GetPropertySetter<TMember>(propertyInfo);

        var customValidator = new MemberCustomValidator<TModel, TMember>(
            propertyInfo, propertyGetter!, propertySetter);

        Add(propertyInfo.Name, customValidator);
        validatorConfig?.Invoke(customValidator);

        return this;
    }

    /// <summary>
    /// Performs, a fast, cached, strongly-typed lookup of a property by its lambda expression.
    /// For example r => r.Id, looks up the Id property.
    /// </summary>
    /// <typeparam name="TMember">The property type.</typeparam>
    /// <param name="memberLambda">The expression that refers to a property.</param>
    /// <param name="propertyInfo">The resulting property.</param>
    /// <returns>Whether the property was found.</returns>
    protected static bool TryGetProperty<TMember>(
        Expression<Func<TModel, TMember>> memberLambda,
        [MaybeNullWhen(false)] out PropertyInfo propertyInfo)
    {
        propertyInfo = null;

        if (memberLambda is null ||
            memberLambda.Body is not MemberExpression memberExpression ||
            memberExpression.Member is not PropertyInfo outputProperty ||
            !outputProperty.CanRead)
        {
            return false;
        }

        propertyInfo = outputProperty;
        return true;
    }

    /// <summary>
    /// Helper method to perform a fast, cached, strongly-typed lookup of the property getter.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <returns>The strongly-typed function to get a value.</returns>
    protected Func<TModel, TProperty?> GetPropertyGetter<TProperty>(PropertyInfo propertyInfo)
    {
        var getterLambda = GetPropertyGetter(propertyInfo);

        return (instance) => instance is not null
            ? (TProperty?)getterLambda!.Invoke(instance)
            : default;
    }

    /// <summary>
    /// Helper method to perform a fast, cached, strongly-typed lookup of the property setter.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <returns>The strongly-typed method to set a value.</returns>
    protected Action<TModel, TProperty?>? GetPropertySetter<TProperty>(PropertyInfo propertyInfo)
    {
        var setterLambda = GetPropertySetter(propertyInfo);
        if (setterLambda is null)
            return default;

        return (instance, value) =>
        {
            if (instance is null)
                return;

            setterLambda!.Invoke(instance, value);
        };
    }

}