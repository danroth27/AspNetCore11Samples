namespace BlazorFeatures.Resources;

// Marker type for the LocalizerProvider configured in Program.cs. The resx files in this
// folder are the resource source IStringLocalizer<ValidationMessages> resolves against.
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

    // Custom async rules still use explicit keys. Built-in attributes use RC1's
    // {AttributeType}_Error resource-name convention.
    public const string UniqueEmailError = nameof(UniqueEmailError);
    public const string UniqueUsernameError = nameof(UniqueUsernameError);
}
