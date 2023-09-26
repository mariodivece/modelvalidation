using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Unosquare.ModelValidation;

public abstract class ModelValidatorBase<T>
    where T : ModelValidatorBase<T>
{
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> CachedGetters = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> CachedSetters = new();
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> CachedProperties = new();

    private readonly Dictionary<string, IList<IFieldValidator>> _fields = new(16, StringComparer.Ordinal);

    protected ModelValidatorBase(Type modelType)
    {
        ModelType = modelType;
    }

    public Type ModelType { get; }

    public IReadOnlyDictionary<string, IList<IFieldValidator>> Fields => _fields;

    public T Add(string fieldName, IFieldValidator validator)
    {
        if (!_fields.TryGetValue(fieldName, out var validators))
            validators = _fields[fieldName] = new List<IFieldValidator>();

        if (validators is List<IFieldValidator> validatorList)
            validatorList.Add(validator);

        return (this as T)!;
    }

    /// <summary>
    /// Adds <see cref="AttributeFieldValidator"/> instances
    /// based on the property attributes found on them.
    /// </summary>
    /// <returns>This instance for fluency.</returns>
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

    public T AddAttributes(string propertyName)
    {
        if (!TryGetProperty(propertyName, out var propertyInfo))
            throw new ArgumentException($"Property '{propertyName}' was not found on type '{ModelType.FullName}'", nameof(propertyName));

        return AddAttributes(propertyInfo);
    }

    public T AddAttributes(PropertyInfo propertyInfo)
    {
        var validators = CreateAttributeValidators(propertyInfo);
        foreach (var validator in validators)
            Add(propertyInfo.Name, validator);

        return (this as T)!;
    }

    public T AddAttribute<TAttribute>(
        PropertyInfo propertyInfo,
        Func<TAttribute> attributeFactory)
        where TAttribute : ValidationAttribute
    {
        var validator = new AttributeFieldValidator(propertyInfo, attributeFactory.Invoke());
        Add(propertyInfo.Name, validator);

        return (this as T)!;
    }

    public T AddAttribute<TAttribute>(
        string propertyName,
        Func<TAttribute> attributeFactory)
        where TAttribute : ValidationAttribute
    {
        if (!TryGetProperty(propertyName, out var propertyInfo))
            throw new ArgumentException($"Property '{propertyName}' was not found on type '{ModelType.FullName}'", nameof(propertyName));

        return AddAttribute(propertyInfo, attributeFactory);
    }

    public T AddCustom(
        string propertyName,
        Action<CustomFieldValidator> validatorConfig)
    {
        if (!TryGetProperty(propertyName, out var propertyInfo))
            throw new ArgumentException($"Property '{propertyName}' was not found on type '{ModelType.FullName}'", nameof(propertyName));

        return AddCustom(propertyInfo, validatorConfig);
    }

    public T AddCustom(
        PropertyInfo propertyInfo,
        Action<CustomFieldValidator> validatorConfig)
    {
        // Work on the property getters and setters
        var propertyGetter = GetPropertyGetter(propertyInfo);
        var propertySetter = GetPropertySetter(propertyInfo);

        var customValidator = new CustomFieldValidator(
            propertyInfo, propertyGetter!, propertySetter);

        Add(propertyInfo.Name, customValidator);
        validatorConfig.Invoke(customValidator);

        return (this as T)!;
    }

    public virtual async ValueTask<ModelValidationResult> ValidateAsync(object modelInstance, IStringLocalizer? localizer = null)
    {
        if (modelInstance is null)
            throw new ArgumentNullException(nameof(modelInstance));

        var validationSummary = new Dictionary<string, IReadOnlyList<ValidationResult>>(Fields.Count, StringComparer.Ordinal);

        if (!Fields.Any())
            return ModelValidationResult.Empty;

        foreach ((var fieldName, var validators) in Fields)
        {
            foreach (var validator in validators)
            {
                var validation = await validator.ValidateAsync(modelInstance, localizer);
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

    public virtual ModelValidationResult Validate(object modelInstance, IStringLocalizer? localizer = null) =>
        ValidateAsync(modelInstance, localizer).AsTask().GetAwaiter().GetResult();

    protected Func<object, object?>? GetPropertyGetter(PropertyInfo propertyInfo)
    {
        if (!propertyInfo.CanRead)
            return null;

        if (!CachedGetters.TryGetValue(propertyInfo, out var getterLambda))
        {
            getterLambda = CreateLambdaGetter(ModelType, propertyInfo);
            CachedGetters[propertyInfo] = getterLambda!;
        }

        return getterLambda;
    }

    protected Action<object, object?>? GetPropertySetter(PropertyInfo propertyInfo)
    {
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

    private static IList<AttributeFieldValidator> CreateAttributeValidators(PropertyInfo property)
    {
        var result = new List<AttributeFieldValidator>();

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
            catch
            {

            }

            // treat as a validation argument
            if (attributeInstance is not ValidationAttribute attribute)
                continue;

            var validator = new AttributeFieldValidator(property, attribute);
            result.Add(validator);
        }

        return result;
    }

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
}

public class ModelValidator
    : ModelValidatorBase<ModelValidator>
{
    public ModelValidator(Type modelType)
        : base(modelType)
    {
    }
}

public class ModelValidator<TModel>
    : ModelValidatorBase<ModelValidator<TModel>>
{
    private const string BadPropertyExpression = "The expression must refer to a public, instance and readable property.";

    public ModelValidator()
        : base(typeof(TModel))
    {
        // placeholder
    }

    public ModelValidator<TModel> AddAttributes<TMember>(Expression<Func<TModel, TMember>> memberLambda)
    {
        if (!TryGetProperty(memberLambda, out var propertyInfo))
            throw new ArgumentException(BadPropertyExpression, nameof(memberLambda));

        return AddAttributes(propertyInfo);
    }

    public ModelValidator<TModel> AddAttribute<TMember, TAttribute>(
        Expression<Func<TModel, TMember>> memberLambda,
        Func<TAttribute> attributeFactory)
        where TAttribute : ValidationAttribute
    {
        if (!TryGetProperty(memberLambda, out var propertyInfo))
            throw new ArgumentException(BadPropertyExpression, nameof(memberLambda));

        return AddAttribute(propertyInfo, attributeFactory);
    }

    public ModelValidator<TModel> AddCustom<TMember>(
        Expression<Func<TModel, TMember>> memberLambda,
        Action<CustomFieldValidator<TModel, TMember>> validatorConfig)
    {
        if (!TryGetProperty(memberLambda, out var propertyInfo))
            throw new ArgumentException(BadPropertyExpression, nameof(memberLambda));

        // Work on the property getters and setters
        var propertyGetter = GetPropertyGetter<TMember>(propertyInfo);
        var propertySetter = GetPropertySetter<TMember>(propertyInfo);

        var customValidator = new CustomFieldValidator<TModel, TMember>(
            propertyInfo, propertyGetter!, propertySetter);

        Add(propertyInfo.Name, customValidator);
        validatorConfig.Invoke(customValidator);

        return this;
    }

    protected static bool TryGetProperty<TMember>(
        Expression<Func<TModel, TMember>> memberLambda,
        [MaybeNullWhen(false)] out PropertyInfo propertyInfo)
    {
        propertyInfo = null;

        if (memberLambda.Body is not MemberExpression memberExpression ||
            memberExpression.Member is not PropertyInfo outputProperty ||
            !outputProperty.CanRead)
        {
            return false;
        }

        propertyInfo = outputProperty;
        return true;
    }

    protected Func<TModel, TProperty?> GetPropertyGetter<TProperty>(PropertyInfo propertyInfo)
    {
        var getterLambda = GetPropertyGetter(propertyInfo);

        return (instance) => instance is not null
            ? (TProperty?)getterLambda!.Invoke(instance)
            : default;
    }

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
