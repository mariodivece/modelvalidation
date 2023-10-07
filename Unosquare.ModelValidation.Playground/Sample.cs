using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Unosquare.ModelValidation.Playground;

internal class Sample
{
    public Sample(
        ILogger<Sample> logger,
        IStringLocalizer<Sample> localizer)
    {
        Logger = logger;
        Text = localizer;
    }

    private ILogger Logger { get; }

    private IStringLocalizer Text { get; }

    public async Task RunAsync()
    {
        var carValidator = new ModelValidator<Car>()
            .AddCustom(r => r.Id, config =>
            {
                config
                    .WithPreValidation((context, value) => 21)
                    .WithValidation((context, value) =>
                    {
                        return value == 21
                            ? context.Pass()
                            : context.Fail();
                    })
                    .WithPostValidation((context, value) => context.SetValue(value));
            })
            .AddRequired(r => r.Name)
            .AddRequired(r => r.Email)
            .AddEmail(r => r.Email)
            .AddAttributes()
            .AddAttributes(r => r.Id)
            .AddAttribute(r => r.Id, () => new RangeAttribute(2, 20) { ErrorMessageResourceName = "Validation.Number.Range" })
;
        var car = new Car()
        {
            Name = string.Empty,
            Email = "xyz@nopet@invlid...com"
        };

        var validation = await carValidator.ValidateAsync(car, Text);
        
        validation.Add("SomeField", "This is some manual error message");
        Logger.LogInformation($"Is Valid: {validation.IsValid}");

        foreach (var fieldName in validation.MemberNames)
            Logger.LogInformation($"{fieldName}: {string.Join("; ", validation[fieldName].Select(c => c.ErrorMessage))}");

        Logger.LogInformation($"Validated Car Name: '{car.Name}'");
    }
}
