// .NET 11 Preview 6 — Async validation for minimal APIs (BACKUP DEMO).
//
// Youssef owns this topic in the standup; this is a standby in case his demo hits
// issues. Minimal API validation now supports asynchronous validators end-to-end
// (dotnet/aspnetcore #66487, #67183). Preview 6 adds async DataAnnotations APIs
// (`AsyncValidationAttribute`, `IAsyncValidatableObject`) and
// `Microsoft.Extensions.Validation` runs them when an endpoint validates a request.
//
// Register `builder.Services.AddValidation();` and the framework validates the
// request before the endpoint runs. See Program.cs for registration + endpoints.

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace ApiFeatures;

// Simulates a slow user repository (DB / remote API) so async validation is meaningful.
public interface IUserService
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}

public sealed class UserService : IUserService
{
    private static readonly HashSet<string> Registered = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin@example.com", "test@example.com", "user@example.com",
    };

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken); // pretend this is a database call
        return Registered.Contains(email);
    }
}

public interface IRoomService
{
    Task<bool> HasAvailabilityAsync(DateOnly date, CancellationToken cancellationToken = default);
}

public sealed class RoomService : IRoomService
{
    public async Task<bool> HasAvailabilityAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);
        // Pretend weekends are fully booked.
        return date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
    }
}

// A custom async validation attribute — the simplest way to add an async rule.
public sealed class UniqueEmailAttribute : AsyncValidationAttribute
{
    // Synchronous IsValid is abstract too. This attribute validates asynchronously only,
    // so throw here to make accidental synchronous validation obvious.
    protected override ValidationResult? IsValid(object? value, ValidationContext context) =>
        throw new InvalidOperationException("Validate this attribute with IsValidAsync.");

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        var users = context.GetRequiredService<IUserService>();
        if (value is string email && await users.EmailExistsAsync(email, cancellationToken))
        {
            return new ValidationResult("That email is already registered.");
        }
        return ValidationResult.Success;
    }
}

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    [UniqueEmail]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(20, MinimumLength = 4)]
    public string DisplayName { get; set; } = "";
}

// Whole-object async validation that spans several properties.
public sealed class ReservationRequest : IAsyncValidatableObject
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    public DateOnly Date { get; set; }

    // Synchronous IValidatableObject. This type validates asynchronously only.
    public IEnumerable<ValidationResult> Validate(ValidationContext context) =>
        throw new InvalidOperationException("Validate this type with ValidateAsync.");

    public async IAsyncEnumerable<ValidationResult> ValidateAsync(
        ValidationContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var rooms = context.GetRequiredService<IRoomService>();
        if (!await rooms.HasAvailabilityAsync(Date, cancellationToken))
        {
            yield return new ValidationResult(
                "No rooms are available on that date.", [nameof(Date)]);
        }
    }
}

public static class Preview6AsyncValidation
{
    public static IEndpointRouteBuilder MapAsyncValidationDemo(this IEndpointRouteBuilder app)
    {
        // Async attribute: POST an already-registered email (e.g. admin@example.com)
        // to see the framework reject it before the handler runs.
        app.MapPost("/register", (RegisterRequest request) =>
            TypedResults.Ok(new { message = $"Registered {request.DisplayName}.", request.Email }))
            .WithName("Register")
            .WithDescription("Async [UniqueEmail] attribute — rejects an already-registered email before the handler runs.");

        // Async whole-object validation: POST a weekend Date to see it rejected.
        app.MapPost("/reservations", (ReservationRequest request) =>
            TypedResults.Ok(new { message = "Reservation confirmed.", request.Email, request.Date }))
            .WithName("CreateReservation")
            .WithDescription("Async IAsyncValidatableObject — rejects a date with no availability (weekends).");

        return app;
    }
}
