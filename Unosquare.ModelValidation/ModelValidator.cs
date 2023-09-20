using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Unosquare.ModelValidation;

public class ModelValidator<TModel>
{
    private static readonly ConcurrentDictionary<PropertyInfo, object> CachedGetters = new();
    private static readonly ConcurrentDictionary<PropertyInfo, object> CachedSetters = new();

    private readonly Dictionary<string, IFieldValidator> _fields = new(16, StringComparer.Ordinal);

    public ModelValidator()
    {
    }

    public IReadOnlyDictionary<string, IFieldValidator> Fields => _fields;

    public FieldValidator<TModel, TMember> Add<TMember>(Expression<Func<TModel, TMember>> memberLambda)
    {
        if (memberLambda.Body is not MemberExpression memberExpression ||
            memberExpression.Member is not PropertyInfo propertyInfo ||
            !propertyInfo.CanRead)
        {
            throw new ArgumentException("The expression must refer to a readable property.", nameof(memberLambda));
        }

        if (_fields.TryGetValue(propertyInfo.Name, out _))
            throw new ArgumentException($"Key '{propertyInfo.Name}' already exists.", nameof(memberLambda));

        // Work on the property getter
        Func<TModel, TMember>? propertyGetter;
        if (!CachedGetters.TryGetValue(propertyInfo, out var getterLambda))
        {
            propertyGetter = memberLambda.Compile();
            CachedGetters[propertyInfo] = propertyGetter;
        }
        else
        {
            propertyGetter = getterLambda as Func<TModel, TMember>;
        }

        // Work on the property setter
        var propertySetter = default(Action<TModel, TMember>?);
        if (propertyInfo.CanWrite)
        {
            if (!CachedSetters.TryGetValue(propertyInfo, out var setterLambda))
            {
                var targetModelVariable = Expression.Variable(typeof(TModel));
                var setterValueVariable = Expression.Variable(typeof(TMember));
                var propertyExpression = Expression.Property(targetModelVariable, propertyInfo);
                var propertySetterExpression = Expression.Assign(propertyExpression, setterValueVariable);

                propertySetter = Expression.Lambda<Action<TModel, TMember>>(
                    propertySetterExpression, targetModelVariable, setterValueVariable).Compile();

                CachedSetters[propertyInfo] = propertySetter;
            }
            else
            {
                propertySetter = setterLambda as Action<TModel, TMember>;
            }
        }

        var field = new FieldValidator<TModel, TMember>(
            propertyInfo, propertyGetter!, propertySetter);

        _fields[propertyInfo.Name] = field;

        return field;
    }

    public async ValueTask<ModelValidationResult> ValidateAsync(TModel instance, IStringLocalizer? localizer)
    {
        if (instance is null)
            throw new ArgumentNullException(nameof(instance));

        var validationSummary = new Dictionary<string, FieldValidationResult>(Fields.Count, StringComparer.Ordinal);

        if (!Fields.Any())
            return ModelValidationResult.Empty;

        foreach ((var fieldName, var validator) in Fields)
        {
            var validation = await validator.ValidateAsync(instance, localizer);
            if (!validation.IsValid)
            {
                validationSummary[fieldName] = validation;
            }
        }

        return new(validationSummary);
    }
}
