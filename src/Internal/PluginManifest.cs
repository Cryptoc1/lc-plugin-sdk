using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LethalCompany.Plugin.Sdk.Internal;

internal sealed class PluginManifest
{
    [JsonPropertyOrder(4)]
    public string[] Dependencies { get; init; } = [];

    [JsonPropertyOrder(1)]
    [StringLength(250)]
    [Required(AllowEmptyStrings = true)]
    public string Description { get; init; } = string.Empty;

    [RegularExpression(@"^([a-zA-Z0-9_]*)?$", ErrorMessage = "The format of the {0} field is not valid.")]
    [Required]
    public required string Name { get; init; }

    [JsonPropertyOrder(2)]
    [JsonPropertyName("version_number")]
    [Required]
    public required SemanticVersion Version { get; init; }

    [JsonPropertyOrder(3)]
    [JsonPropertyName("website_url")]
    [WebsiteUrl]
    public string WebsiteUrl { get; init; } = string.Empty;

    public bool TryValidate([NotNullWhen(false)] out IReadOnlyCollection<ValidationResult>? errors)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(this, new(this), results, true))
        {
            errors = results;
            return false;
        }

        errors = default;
        return true;
    }
}

internal sealed class WebsiteUrlAttribute : ValidationAttribute
{
    private readonly UrlAttribute url = new();

    public WebsiteUrlAttribute() : base("The field {0} is not a fully qualified http or https url.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            return url.IsValid(str);
        }

        return true;
    }
}
