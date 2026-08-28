namespace BlazorFeatures.Data;

// Interactive Blazor form model used by /async-validation.
//
// Every rule here is a DataAnnotations attribute - including the *asynchronous*
// uniqueness checks, which are [UniqueUsername] / [UniqueEmail] (see
// AsyncValidationAttributes.cs). In .NET 11 DataAnnotations validation is async
// end to end, so the model stays fully declarative and the form needs no
// hand-written validation plumbing.
//
// [ValidatableType] + AddValidation() (Program.cs) opt this type into
// Microsoft.Extensions.Validation, which is what gives DataAnnotationsValidator
// its async path. Without them, Blazor falls back to synchronous validation and
// the async attributes never run.
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

[ValidatableType]
public class RegistrationModel
{
    [Required]
    [StringLength(20, MinimumLength = 4)]
    [UniqueUsername]
    [Display(Name = nameof(Resources.ValidationMessages.Username))]
    public string Username { get; set; } = "";

    [Required]
    [EmailAddress]
    [UniqueEmail]
    [Display(Name = nameof(Resources.ValidationMessages.Email))]
    public string Email { get; set; } = "";

    [Required]
    [Range(13, 120)]
    [Display(Name = nameof(Resources.ValidationMessages.Age))]
    public int? Age { get; set; }

    [Url]
    [Display(Name = nameof(Resources.ValidationMessages.Website))]
    public string? Website { get; set; }
}
