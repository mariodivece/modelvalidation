using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Unosquare.ModelValidation;

public class ModelValidator<TModel>
{
    private static readonly ConcurrentDictionary<PropertyInfo, object> CachedGetters = new();
    private static readonly ConcurrentDictionary<PropertyInfo, object> CachedSetters = new();

    private readonly Dictionary<string, IList<IFieldValidator>> _fields = new(16, StringComparer.Ordinal);

    public ModelValidator()
    {
    }

    public IReadOnlyDictionary<string, IList<IFieldValidator>> Fields => _fields;

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

    public ModelValidator<TModel> AddFromAttributes()
    {
        var properties = typeof(TModel).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties)
        {
            var validators = CreateAttributeValidators(property);
            foreach (var validator in validators)
                AddFieldValidator(property.Name, validator);
        }

        return this;
    }

    public ModelValidator<TModel> AddFromAttributes<TMember>(Expression<Func<TModel, TMember>> memberLambda)
    {
        if (memberLambda.Body is not MemberExpression memberExpression ||
            memberExpression.Member is not PropertyInfo propertyInfo ||
            !propertyInfo.CanRead)
        {
            throw new ArgumentException("The expression must refer to a readable property.", nameof(memberLambda));
        }

        var validators = CreateAttributeValidators(propertyInfo);
        foreach (var validator in validators)
            AddFieldValidator(propertyInfo.Name, validator);

        return this;
    }

    public FieldValidator<TModel, TMember> AddCustom<TMember>(Expression<Func<TModel, TMember>> memberLambda)
    {
        if (memberLambda.Body is not MemberExpression memberExpression ||
            memberExpression.Member is not PropertyInfo propertyInfo ||
            !propertyInfo.CanRead)
        {
            throw new ArgumentException("The expression must refer to a readable property.", nameof(memberLambda));
        }

        // Work on the property getters and setters
        var propertyGetter = GetPropertyGetter(propertyInfo, memberLambda);
        var propertySetter = GetPropertySetter<TMember>(propertyInfo);

        var validator = new FieldValidator<TModel, TMember>(
            propertyInfo, propertyGetter!, propertySetter);

        AddFieldValidator(propertyInfo.Name, validator);

        return validator;
    }

    public async ValueTask<ModelValidationResult> ValidateAsync(TModel instance, IStringLocalizer? localizer)
    {
        if (instance is null)
            throw new ArgumentNullException(nameof(instance));

        var validationSummary = new Dictionary<string, ValidationResult>(Fields.Count, StringComparer.Ordinal);

        if (!Fields.Any())
            return ModelValidationResult.Empty;

        foreach ((var fieldName, var validators) in Fields)
        {
            foreach (var validator in validators)
            {
                var validation = await validator.ValidateAsync(instance, localizer);
                if (validation is not null)
                {
                    validationSummary[fieldName] = validation;
                }
            }
        }

        return new(validationSummary);
    }

    private void AddFieldValidator(string fieldName, IFieldValidator validator)
    {
        if (!_fields.TryGetValue(fieldName, out var validators))
            validators = _fields[fieldName] = new List<IFieldValidator>();

        if (validators is List<IFieldValidator> validatorList)
            validatorList.Add(validator);
    }

    private static Func<TModel, TProperty> GetPropertyGetter<TProperty>(
        PropertyInfo propertyInfo,
        Expression<Func<TModel, TProperty>> memberLambda)
    {
        if (!CachedGetters.TryGetValue(propertyInfo, out var getterLambda))
        {
            getterLambda = memberLambda.Compile();
            CachedGetters[propertyInfo] = getterLambda;
        }

        return (getterLambda as Func<TModel, TProperty>)!;
    }

    private static Action<TModel, TProperty>? GetPropertySetter<TProperty>(PropertyInfo propertyInfo)
    {
        if (!propertyInfo.CanWrite)
            return null;

        if (!CachedSetters.TryGetValue(propertyInfo, out var setterLambda))
        {
            var targetModelVariable = Expression.Variable(typeof(TModel));
            var setterValueVariable = Expression.Variable(typeof(TProperty));
            var propertyExpression = Expression.Property(targetModelVariable, propertyInfo);
            var propertySetterExpression = Expression.Assign(propertyExpression, setterValueVariable);

            setterLambda = Expression.Lambda<Action<TModel, TProperty>>(
                propertySetterExpression, targetModelVariable, setterValueVariable).Compile();

            CachedSetters[propertyInfo] = setterLambda;
        }


        return setterLambda as Action<TModel, TProperty>;
    }
}
