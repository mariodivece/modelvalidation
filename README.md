# A Flexible Model Validation

An easy-to-use formatter, validator and postprocessor for user input management.

[![NuGet](https://img.shields.io/nuget/dt/Unosquare.ModelValidation)](https://www.nuget.org/packages/Unosquare.ModelValidation)

## Motivation

The standard and documented model validation techniques are typically quite inflexible (in my view). If you
look at <a href="https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-7.0">
the documentation</a>, you will notice a few things:

1. Model validation is mostly based on property attributes
1. Adding custom validation requires the implamentation of a ```ValidationAttribute``` and the scope of the
validation logic tends to be limited. For example, validating if a user exists may require a call to a database
or an API. While this is possible with the ```RemoteAttribute```, it introduces unnecessary complexity.
1. The default validation API does not support strongly-typed access to the model.
1. Adding validation to classes for which attributes you cannot modify (i.e. external libraries) does not feel
streamlined.
1. There is no bult-in functionality to format input and validate it once it has been formatted.
1. Validation of only part of the model is not supported. It feels like an all-or-none approach that proves
difficult in for example, wizard-oriented scenarios where multi-step data is stored in a single model.

## Usage

Please look at the following usage examples below. Notice this API provides a much more flexible approach
to validation than the default one. You can define multiple validators for any model class and you can manually
change the results as you please.

```cs

// Assume the following model class.
public record Employee
{
    [Range(1, 10, ErrorMessage = "Unfortunately, this field '{0}' must have a value between {1} and {2}.")]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }
}

// Create a model validator (there is a non strongly-typed version that you can also use)
var validatior = new ModelValidator<Employee>();

// You can now add the existing validation attributes, either one property at a time:
validator.AddAttributes();

// or one by one (you can use string fro property names if you prefer)
validator.AddAttributes(r => r.Id);

// or add pre-built validation attributes
validator
    .AddRequired(r => r.Name)
    .AddEmail(r => r.Email);

// or clear existing validation logic and add ValidationAttribute instances without the need
// to modify the class.
validator.Remove(r => r.Id)
validator.AddAttribute(r => r.Id, () => new RangeAttribute(2, 20);

// And last but not least -- add totally custom input formatting and validation
validator.AddCustom(r => r.Id, config =>
{
    config
        .WithPreValidation((context, value) => 21)
        .WithValidation((context, value) =>
        {
            // you are welcome to make this call async
            return value == 21
                ? context.Pass()
                : context.Fail();
        })
        .WithPostValidation((context, value) => context.SetValue(value));
});

// now you can run your validator as follows:
var target = new Employee();
var validation = await validator.ValidateAsync(target); // there is a synchronous version as well.

// And get either individual results
if (validation.IsInvalid(r => r.Id))
    Console.WriteLine(validation.ErrorMessage(r => r.Id));

// or total model validation
if (validation.IsValid)
    Console.WriteLine("Your model is valid");

// or manually add errors external to the model validator
validation.Add("SomeField", "This is some manual error message");

// which then would make validation.IsValid, false
if (validation.IsValid)
    Console.WriteLine("Your model is valid");

```