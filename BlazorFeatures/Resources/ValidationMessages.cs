namespace BlazorFeatures.Resources;

// Marker type for AddValidationLocalization<T>(). The resx files in this folder
// (ValidationMessages.resx + culture-specific siblings) are the resource source
// IStringLocalizer<ValidationMessages> resolves against.
internal sealed class ValidationMessages
{
    // Display name keys - referenced via nameof(...) on [Display(Name = ...)].
    public const string ContactName = nameof(ContactName);
    public const string ContactEmail = nameof(ContactEmail);
    public const string ContactMessage = nameof(ContactMessage);
    public const string Username = nameof(Username);
    public const string Email = nameof(Email);
    public const string Age = nameof(Age);
    public const string Website = nameof(Website);

    // Error message keys - referenced via nameof(...) on ValidationAttribute.ErrorMessage.
    public const string RequiredError = nameof(RequiredError);
    public const string StringLengthError = nameof(StringLengthError);
    public const string EmailError = nameof(EmailError);
    public const string RangeError = nameof(RangeError);
    public const string UrlError = nameof(UrlError);
    public const string UniqueEmailError = nameof(UniqueEmailError);
    public const string UniqueUsernameError = nameof(UniqueUsernameError);
}
