// Async DataAnnotations attributes for the /async-validation Blazor form.
//
// These are the *same* .NET 11 hooks the ApiFeatures project uses for minimal APIs
// (see ApiFeatures/AsyncValidation.cs): derive from AsyncValidationAttribute and
// override IsValidAsync. Nothing here is Blazor-specific - the attribute resolves
// its dependencies from the ValidationContext, so the identical rule runs on the
// server for an API request and in a Blazor form.
//
// Blazor routes these through Microsoft.Extensions.Validation, which requires the
// model to be marked [ValidatableType] and the app to call AddValidation() (see
// Program.cs). DataAnnotationsValidator then registers each field check as a
// validation task so the framework can track pending/faulted state and supersede
// stale checks when the user keeps typing.

using System.ComponentModel.DataAnnotations;
using BlazorFeatures.Resources;

namespace BlazorFeatures.Data;

public sealed class UniqueUsernameAttribute : AsyncValidationAttribute
{
    // ErrorMessage doubles as the resource key: AddValidationLocalization<T>() looks it
    // up in ValidationMessages.resx (and its culture-specific siblings), falling back to
    // the ValidationResult message below when no resource matches.
    public UniqueUsernameAttribute()
        => ErrorMessage = nameof(ValidationMessages.UniqueUsernameError);

    // Synchronous IsValid is abstract on ValidationAttribute. This rule validates
    // asynchronously only, so throw rather than returning Success: a synchronous
    // validator would otherwise silently skip the uniqueness check and report the
    // model as valid. Fail loudly instead of quietly passing.
    protected override ValidationResult? IsValid(object? value, ValidationContext context) =>
        throw new InvalidOperationException("Validate this attribute with IsValidAsync.");

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        var users = context.GetRequiredService<UserService>();
        if (value is string username &&
            !string.IsNullOrWhiteSpace(username) &&
            await users.IsUsernameTakenAsync(username, cancellationToken))
        {
            return new ValidationResult("This username is already taken.", [context.MemberName!]);
        }

        return ValidationResult.Success;
    }
}

public sealed class UniqueEmailAttribute : AsyncValidationAttribute
{
    public UniqueEmailAttribute()
        => ErrorMessage = nameof(ValidationMessages.UniqueEmailError);

    protected override ValidationResult? IsValid(object? value, ValidationContext context) =>
        throw new InvalidOperationException("Validate this attribute with IsValidAsync.");

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        var users = context.GetRequiredService<UserService>();
        if (value is string email &&
            !string.IsNullOrWhiteSpace(email) &&
            await users.IsEmailTakenAsync(email, cancellationToken))
        {
            return new ValidationResult("This email is already registered.", [context.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
