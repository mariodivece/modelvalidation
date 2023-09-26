using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Unosquare.ModelValidation;

public static class ValidationExtensions
{
    private static readonly EmailAddressAttribute DataValidatorEmailAddress = new();
    private static readonly RequiredAttribute DataValidatorRequired = new();

    public static ModelValidator<TModel> AddEmail<TModel>(
        this ModelValidator<TModel> validator, Expression<Func<TModel, string?>> memberLambda)
    {
        _ = validator.AddCustom(memberLambda, config =>
        {
            config.WithInputFilter((context, originalValue) =>
             {
                 if (originalValue is null)
                     return originalValue;

                 return originalValue.Trim().ToLowerInvariant();
             })
            .WithValidation((context, filteredValue) =>
            {
                if (filteredValue is null)
                    return context.Pass();

                if (!DataValidatorEmailAddress.IsValid(filteredValue))
                    return context.Fail(context.Localize("Validation.Email.BadFormat", "Invalid Email format."));

                return context.Pass();
            })
            .WithPostSuccess((context, filteredValue) =>
            {
                _ = context.TrySetValue(filteredValue);
            });
        });

        return validator;
    }

    public static ModelValidator<TModel> AddRequired<TModel, TMember>(
        this ModelValidator<TModel> validator, Expression<Func<TModel, TMember?>> memberLambda)
    {
        _ = validator.AddCustom(memberLambda, config =>
        {
            config.WithValidation((context, filteredValue) =>
             {
                 return DataValidatorRequired.IsValid(filteredValue) ? context.Pass() : context.Fail(
                     context.Localize("Validation.Required.NotNull", "Field is required."));
             });
        });

        return validator;
    }
}
