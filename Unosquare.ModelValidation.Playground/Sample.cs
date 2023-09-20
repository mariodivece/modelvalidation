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
        var validator = new ModelValidator<Car>();

        validator.Add(r => r.Name)
            .WithInputFilter((ctx, originalValue) =>
                originalValue.Trim().ToUpperInvariant())
            .WithValidation(async (ctx, currentValue) =>
                currentValue.StartsWith("X")
                    ? await ctx.Pass()
                    : await ctx.Fail(ctx.Localizer!["ErrorMessage"]))
            .WithPostSuccess((ctx, validatedValue) =>
            {
                ctx.SetValue(validatedValue);
            });

        var car = new Car() { Name = "   Sample Car   " };

        Logger.LogInformation($"Initial Car Name: '{car.Name}'");

        var v = await validator.ValidateAsync(car, Text);
        Logger.LogInformation($"Is Valid: {v.IsValid}");
        foreach (var fieldName in v.FieldNames)
        {
            Logger.LogInformation($"{fieldName}: {v[fieldName]}");
        }

        Logger.LogInformation($"Validated Car Name: '{car.Name}'");
    }
}
