namespace BlazorFeatures.Data;

// Interactive Blazor form model used by /async-validation.
// Uniqueness checks (username/email) are not modeled as ValidationAttributes -
// they run via the new EditContext.OnValidationRequestedAsync event and the
// per-field EditContext.AddValidationTask API (PR #66526), wired up inline in
// AsyncValidationDemo.razor against a UserService. This keeps the model
// declarative and lets the async work participate in the framework's
// pending/faulted state tracking.
using System.ComponentModel.DataAnnotations;

[ValidatableType]
public class RegistrationModel
{
    [Required(ErrorMessage = nameof(Resources.ValidationMessages.RequiredError))]
    [StringLength(20, MinimumLength = 4, ErrorMessage = nameof(Resources.ValidationMessages.StringLengthError))]
    [Display(Name = nameof(Resources.ValidationMessages.Username))]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = nameof(Resources.ValidationMessages.RequiredError))]
    [EmailAddress(ErrorMessage = nameof(Resources.ValidationMessages.EmailError))]
    [Display(Name = nameof(Resources.ValidationMessages.Email))]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = nameof(Resources.ValidationMessages.RequiredError))]
    [Range(13, 120, ErrorMessage = nameof(Resources.ValidationMessages.RangeError))]
    [Display(Name = nameof(Resources.ValidationMessages.Age))]
    public int? Age { get; set; }

    [Url(ErrorMessage = nameof(Resources.ValidationMessages.UrlError))]
    [Display(Name = nameof(Resources.ValidationMessages.Website))]
    public string? Website { get; set; }
}
