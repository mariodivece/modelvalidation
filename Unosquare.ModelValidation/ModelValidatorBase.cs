using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Unosquare.ModelValidation;

/// <summary>
/// Represents a base class for model validator.
/// </summary>
/// <typeparam name="T">A type parameter for fluent API support (curious visitor pattern).</typeparam>
public abstract class ModelValidatorBase<T>
    where T : ModelValidatorBase<T>
{
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> CachedGetters = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> CachedSetters = new();
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> CachedProperties = new();

    private readonly Dictionary<string, IList<IMemberValidator>> _fields = new(16, StringComparer.Ordinal);

    /// <summary>
    /// Creates a new instance of the <see cref="ModelValidatorBase{T}"/> class.
    /// </summary>
    /// <param name="modelType">The type of the model to validate.</param>
    protected ModelValidatorBase(Type modelType)
    {
        ModelType = modelType;
    }

    /// <summary>
    /// Gets the type of the model this validator is built for.
    /// </summary>
    public Type ModelType { get; }

    /// <summary>
    /// Gets the members that participate in the model validator and their
    /// associated <see cref="IMemberValidator"/> objects.
    /// </summary>
    public IReadOnlyDictionary<string, IList<IMemberValidator>> Members => _fields;

    /// <summary>
    /// Removes validators from the specified member.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <returns>This instance for fluent API support.</returns>
    public T Remove(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName))
            return (this as T)!;

        _fields.Remove(memberName);

        return (this as T)!;
    }

    /// <summary>
    /// Adds a validator to the specified field name.
    /// </summary>
    /// <param name="memberName">The unique name of the member.</param>
    /// <param name="validator">The validator object to associate the field name with.</param>
    /// <returns>This instance for fluent API support.</returns>
    public T Add(string memberName, IMemberValidator validator)
    {
        if (!_fields.TryGetValue(memberName, out var validators))
            validators = _fields[memberName] = new List<IMemberValidator>();

        if (validators is List<IMemberValidator> validatorList)
            validatorList.Add(validator);

        return (this as T)!;
    }

    /// <summary>
    /// Adds <see cref="MemberAttributeValidator"/> instances
    /// based on the property attributes found on them.
    /// </summary>
    /// <returns>This instance for fluent API support.</returns>
    public T AddAttributes()
    {
        var properties = ModelType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties)
        {
            if (!property.CanRead)
                continue;

            var validators = CreateAttributeValidators(property);
            foreach (var validator in validators)
                Add(property.Name, validator);
        }

        return (this as T)!;
    }

    /// <summary>
    /// Adds <see cref="MemberAttributeValidator"/> instances
    /// for a single property by providing the name of the property.
    /// </summary>
    /// <param name="propertyName">The name of the property to look for.</param>
    /// <returns>This instance for fluent API support.</returns>
    public T AddAttributes(string propertyName)
    {
        if (!TryGetProperty(propertyName, out var propertyInfo))
            throw new ArgumentException($"Property '{propertyName}' was not found on type '{ModelType.FullName}'", nameof(propertyName));

        return AddAttributes(propertyInfo);
    }

    /// <summary>
    /// Adds <see cref="MemberAttributeValidator"/> instances
    /// for a single property by providing the name of the property.
    /// </summary>
    /// <param name="propertyInfo">The property to look for.</param>
    /// <returns>This instance for fluent API support.</returns>
    public T AddAttributes(PropertyInfo propertyInfo)
    {
        if (propertyInfo is null)
            throw new ArgumentNullException(nameof(propertyInfo));

        var validators = CreateAttributeValidators(propertyInfo);
        foreach (var validator in validators)
            Add(propertyInfo.Name, validator);

        return (this as T)!;
    }

    /// <summary>
    /// Adds a validator based on a <see cref="ValidationAttribute"/> instance.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type.</typeparam>
    /// <param name="propertyInfo">The associated property.</param>
    /// <param name="attributeFactory">The function that creates an instance of the attribute.</param>
    /// <returns>This instance for fluent API support.</returns>
    public T AddAttribute<TAttribute>(
        PropertyInfo propertyInfo,
        Func<TAttribute> attributeFactory)
        where TAttribute : ValidationAttribute
    {
        if (attributeFactory is null)
            throw new ArgumentNullException(nameof(attributeFactory));

        var validator = new MemberAttributeValidator(propertyInfo, attributeFactory.Invoke());
        Add(propertyInfo.Name, validator);

        return (this as T)!;
    }

    /// <summary>
    /// Adds a validator based on a <see cref="ValidationAttribute"/> instance.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type.</typeparam>
    /// <param name="propertyName">The associated property.</param>
    /// <param name="attributeFactory">The function that creates an instance of the attribute.</param>
    /// <returns>This instance for fluent API support.</returns>
    public T AddAttribute<TAttribute>(
        string propertyName,
        Func<TAttribute> attributeFactory)
        where TAttribute : ValidationAttribute
    {
        if (!TryGetProperty(propertyName, out var propertyInfo))
            throw new ArgumentException($"Property '{propertyName}' was not found on type '{ModelType.FullName}'", nameof(propertyName));

        return AddAttribute(propertyInfo, attributeFactory);
    }

    /// <summary>
    /// Adds a custom validator to the associated property.
    /// </summary>
    /// <param name="propertyName">The associated property.</param>
    /// <param name="validatorConfig">The action containing an instance of the custom validator to configure.</param>
    /// <returns>This instance for fluent API support.</returns>
    public T AddCustom(
        string propertyName,
        Action<MemberCustomValidator> validatorConfig)
    {
        if (!TryGetProperty(propertyName, out var propertyInfo))
            throw new ArgumentException($"Property '{propertyName}' was not found on type '{ModelType.FullName}'", nameof(propertyName));

        return AddCustom(propertyInfo, validatorConfig);
    }

    /// <summary>
    /// Adds a custom validator to the associated property.
    /// </summary>
    /// <param name="propertyInfo">The associated property.</param>
    /// <param name="validatorConfig">The action containing an instance of the custom validator to configure.</param>
    /// <returns>This instance for fluent API support.</returns>
    public T AddCustom(
        PropertyInfo propertyInfo,
        Action<MemberCustomValidator> validatorConfig)
    {
        if (propertyInfo is null)
            throw new ArgumentNullException(nameof(propertyInfo));

        if (validatorConfig is null)
            throw new ArgumentNullException(nameof(validatorConfig));

        // Work on the property getters and setters
        var propertyGetter = GetPropertyGetter(propertyInfo);
        var propertySetter = GetPropertySetter(propertyInfo);

        var customValidator = new MemberCustomValidator(
            propertyInfo, propertyGetter!, propertySetter);

        Add(propertyInfo.Name, customValidator);
        validatorConfig.Invoke(customValidator);

        return (this as T)!;
    }

    /// <summary>
    /// Executes validation logic for all the registered members and returns a validation result.
    /// </summary>
    /// <param name="modelInstance">The instance fo the model to validate.</param>
    /// <param name="localizer">The optional string localizer.</param>
    /// <returns>The validation result.</returns>
    public virtual async ValueTask<ModelValidationResult> ValidateAsync(object modelInstance, IStringLocalizer? localizer = null)
    {
        if (modelInstance is null)
            throw new ArgumentNullException(nameof(modelInstance));

        var validationSummary = new Dictionary<string, IReadOnlyList<ValidationResult>>(Members.Count, StringComparer.Ordinal);

        if (!Members.Any())
            return ModelValidationResult.Empty;

        foreach ((var fieldName, var validators) in Members)
        {
            foreach (var validator in validators)
            {
                var validation = await validator.ValidateAsync(modelInstance, localizer).ConfigureAwait(false);
                if (validation is not null)
                {
                    if (!validationSummary.TryGetValue(fieldName, out var fieldValidations))
                    {
                        fieldValidations = new List<ValidationResult>();
                        validationSummary[fieldName] = fieldValidations;
                    }

                    (fieldValidations as List<ValidationResult>)!.Add(validation);
                }
            }
        }

        return new(validationSummary);
    }

    /// <summary>
    /// Executes validation logic for all the registered members and returns a validation result.
    /// </summary>
    /// <param name="modelInstance">The instance fo the model to validate.</param>
    /// <param name="localizer">The optional string localizer.</param>
    /// <returns>The validation result.</returns>
    public virtual ModelValidationResult Validate(object modelInstance, IStringLocalizer? localizer = null) =>
        ValidateAsync(modelInstance, localizer).AsTask().GetAwaiter().GetResult();

    /// <summary>
    /// A helper function that performs a fast, cached lookup for a property.
    /// </summary>
    /// <param name="propertyName">The name of the property to look for.</param>
    /// <param name="propertyInfo">The output property information.</param>
    /// <returns>Whether the operation succeeded.</returns>
    protected bool TryGetProperty(
        string propertyName,
        [MaybeNullWhen(false)] out PropertyInfo propertyInfo)
    {
        if (!CachedProperties.TryGetValue(ModelType, out var properties))
        {
            var modelProperties = ModelType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead)
                .ToArray();

            properties = new Dictionary<string, PropertyInfo>(modelProperties.Length, StringComparer.Ordinal);
            CachedProperties[ModelType] = properties;

            foreach (var property in modelProperties)
            {
                if (property.GetIndexParameters().Length > 0)
                    continue;

                properties[property.Name] = property;
            }
        }

        return properties.TryGetValue(propertyName, out propertyInfo);
    }

    /// <summary>
    /// A helper function that performs a fast, cached lookup for a property getter.
    /// </summary>
    /// <param name="propertyInfo">The associated property.</param>
    /// <returns>A function that reads the property value.</returns>
    protected Func<object, object?>? GetPropertyGetter(PropertyInfo propertyInfo)
    {
        if (propertyInfo is null)
            return null;

        if (!propertyInfo.CanRead)
            return null;

        if (!CachedGetters.TryGetValue(propertyInfo, out var getterLambda))
        {
            getterLambda = CreateLambdaGetter(ModelType, propertyInfo);
            CachedGetters[propertyInfo] = getterLambda!;
        }

        return getterLambda;
    }

    /// <summary>
    /// A helper function that performs a fast, cached lookup for a property setter.
    /// </summary>
    /// <param name="propertyInfo">The associated property.</param>
    /// <returns>A function that writes the property value.</returns>
    protected Action<object, object?>? GetPropertySetter(PropertyInfo propertyInfo)
    {
        if (propertyInfo is null)
            return null;

        if (!propertyInfo.CanWrite)
            return null;

        if (!CachedSetters.TryGetValue(propertyInfo, out var setterLambda))
        {
            setterLambda = CreateLambdaSetter(ModelType, propertyInfo);
            CachedSetters[propertyInfo] = setterLambda!;
        }

        return setterLambda;
    }

    private static Func<object, object?>? CreateLambdaGetter(Type instanceType, PropertyInfo propertyInfo)
    {
        if (!propertyInfo.CanRead)
            return null;

        var instanceParameter = Expression.Parameter(typeof(object), "instance");
        var typedInstance = Expression.Convert(instanceParameter, instanceType);
        var property = Expression.Property(typedInstance, propertyInfo);
        var convert = Expression.Convert(property, typeof(object));
        var lambdaGetter = Expression
            .Lambda<Func<object, object?>>(convert, instanceParameter)
            .Compile();

        return lambdaGetter;
    }

    private static Action<object, object?>? CreateLambdaSetter(Type instanceType, PropertyInfo propertyInfo)
    {
        if (!propertyInfo.CanWrite)
            return null;

        var instanceParameter = Expression.Parameter(typeof(object), "instance");
        var valueParameter = Expression.Parameter(typeof(object), "value");

        var typedInstance = Expression.Convert(instanceParameter, instanceType);
        var property = Expression.Property(typedInstance, propertyInfo);
        var propertyValue = Expression.Convert(valueParameter, propertyInfo.PropertyType);

        var body = Expression.Assign(property, propertyValue);
        var lambdaSetter = Expression
            .Lambda<Action<object, object?>>(body, instanceParameter, valueParameter)
            .Compile();

        return lambdaSetter;
    }

    private static IList<MemberAttributeValidator> CreateAttributeValidators(PropertyInfo property)
    {
        var result = new List<MemberAttributeValidator>();

        var attributeList = property.GetCustomAttributesData();
        foreach (var attributeData in attributeList)
        {
            var dataAttributeType = attributeData.AttributeType;
            if (!dataAttributeType.IsSubclassOf(typeof(ValidationAttribute)))
                continue;

            object? attributeInstance = null;

            try
            {
                // Create and instance of the attribute.
                attributeInstance = attributeData.Constructor.Invoke(attributeData.ConstructorArguments.Select(c => c.Value).ToArray());

                // Copy named arguments.
                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    if (namedArgument.MemberInfo is PropertyInfo namedProperty)
                        namedProperty.SetValue(attributeInstance, namedArgument.TypedValue.Value);
                }
            }
#pragma warning disable CA1031
            catch
            {
                // ignore
            }
#pragma warning restore CA1031

            // treat as a validation argument
            if (attributeInstance is not ValidationAttribute attribute)
                continue;

            var validator = new MemberAttributeValidator(property, attribute);
            result.Add(validator);
        }

        return result;
    }

}
