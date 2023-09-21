using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

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
            .AddRequired(r => r.Name)
            .AddRequired(r => r.Email)
            .AddEmail(r => r.Email)
            .AddFromAttributes()
            .AddFromAttributes(r => r.Id);

        var car = new Car() { 
            Name = string.Empty,
            Email ="xyz@nopet@invlid...com" };

        var validation = await carValidator.ValidateAsync(car, Text);
        Logger.LogInformation($"Is Valid: {validation.IsValid}");

        foreach (var fieldName in validation.FieldNames)
            Logger.LogInformation($"{fieldName}: {validation[fieldName]}");

        Logger.LogInformation($"Validated Car Name: '{car.Name}'");
    }
}
