namespace Unosquare.ModelValidation;

/// <summary>
/// A delegate that represents a filtering action that reads the original value
/// and returns a new value that will be passed to the <see cref="ValidationActionAsync{TModel, TMember}"/>
/// method.
/// </summary>
/// <typeparam name="TMember">The type of the property.</typeparam>
/// <typeparam name="TModel">The type of the model.</typeparam>
/// <param name="context">The validation context.</param>
/// <param name="originalValue">The original value that was read from the property.</param>
/// <returns>The value that will be passed through the validation logic.</returns>
public delegate ValueTask<TMember> PreValidationFormatAsync<TMember, TModel>(FieldValidatorContext<TModel, TMember> context, TMember originalValue);

public delegate TMember PreValidationFormat<TMember, TModel>(FieldValidatorContext<TModel, TMember> context, TMember originalValue);


public delegate ValueTask<FieldValidationResult> ValidationActionAsync<TModel, TMember>(FieldValidatorContext<TModel, TMember> context, TMember currentValue);

public delegate ValueTask PostValidationActionAsync<TModel, TMember>(FieldValidatorContext<TModel, TMember> context, TMember validatedValue);

public delegate void PostValidationAction<TModel, TMember>(FieldValidatorContext<TModel, TMember> context, TMember validatedValue);
