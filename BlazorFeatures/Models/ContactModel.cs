using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace BlazorFeatures.Models;

// SSR Blazor form model used by /client-validation. Combines:
//   - [ValidatableType] so Microsoft.Extensions.Validation registers an IValidatableInfo,
//     which makes DataAnnotationsValidator route through the new pipeline and pick up
//     IValidationLocalizer for both display names and error messages.
//   - Standard DataAnnotations attributes whose ErrorMessage values are *resource keys*
//     looked up via the same localizer.
// In the rendered HTML these attributes produce data-val-* attributes (PR #66441) that
// the bundled validation JS library (PR #66420, shipped inside blazor.web.js) enforces
// client-side - no server round-trip required.
[ValidatableType]
public class ContactModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = nameof(Resources.ValidationMessages.ContactName))]
    public string? Name { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = nameof(Resources.ValidationMessages.ContactEmail))]
    public string? Email { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 10)]
    [Display(Name = nameof(Resources.ValidationMessages.ContactMessage))]
    public string? Message { get; set; }
}
