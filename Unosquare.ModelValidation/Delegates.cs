using System.ComponentModel.DataAnnotations;

namespace Unosquare.ModelValidation;

/// <summary>
/// A delegate that represents a filtering action that reads the original value
/// and returns a new value that will be passed to the <see cref="ValidationMethodAsync{TModel, TMember}"/>
/// method.
/// </summary>
/// <typeparam name="TMember">The type of the property.</typeparam>
/// <typeparam name="TModel">The type of the model.</typeparam>
/// <param name="context">The validation context.</param>
/// <param name="originalValue">The original value that was read from the property.</param>
/// <returns>The value that will be passed through the validation logic.</returns>
public delegate ValueTask<TMember?> PreValidationValueAsync<TMember, TModel>(MemberValidatorContext<TModel, TMember?> context, TMember? originalValue);

/// <summary>
/// A delegate that represents a filtering action that reads the original value
/// and returns a new value that will be passed to the <see cref="ValidationMethodAsync{TModel, TMember}"/>
/// method.
/// </summary>
/// <typeparam name="TMember">The type of the property.</typeparam>
/// <typeparam name="TModel">The type of the model.</typeparam>
/// <param name="context">The validation context.</param>
/// <param name="originalValue">The original value that was read from the property.</param>
/// <returns>The value that will be passed through the validation logic.</returns>
public delegate TMember? PreValidationValue<TMember, TModel>(MemberValidatorContext<TModel, TMember?> context, TMember? originalValue);

/// <summary>
/// A delegate that represents a value validation.
/// </summary>
/// <typeparam name="TModel">The owning model type.</typeparam>
/// <typeparam name="TMember">The member's type.</typeparam>
/// <param name="context">The associated validation context.</param>
/// <param name="currentValue">The current value to validate.</param>
/// <returns>A validation result.</returns>
public delegate ValueTask<ValidationResult?> ValidationMethodAsync<TModel, TMember>(MemberValidatorContext<TModel, TMember?> context, TMember? currentValue);

/// <summary>
/// A delegate that represents actions to take when value validation completes.
/// </summary>
/// <typeparam name="TModel">The owning model type.</typeparam>
/// <typeparam name="TMember">The member's type.</typeparam>
/// <param name="context">The associated validation context.</param>
/// <param name="validatedValue">The validated value.</param>
/// <returns>An awaitable value task.</returns>
public delegate ValueTask PostValidationActionAsync<TModel, TMember>(MemberValidatorContext<TModel, TMember?> context, TMember? validatedValue);

/// <summary>
/// A delegate that represents actions to take when value validation completes.
/// </summary>
/// <typeparam name="TModel">The owning model type.</typeparam>
/// <typeparam name="TMember">The member's type.</typeparam>
/// <param name="context">The associated validation context.</param>
/// <param name="validatedValue">The validated value.</param>
public delegate void PostValidationAction<TModel, TMember>(MemberValidatorContext<TModel, TMember?> context, TMember? validatedValue);
