using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using Zafiro.UI;

namespace EffectHub.Sections.Settings;

public partial class ApiUrlPromptViewModel : ReactiveValidationObject, IValidatable
{
    [Reactive] private string url = "";

    public ApiUrlPromptViewModel(string? initialUrl = null)
    {
        Url = initialUrl ?? string.Empty;

        this.ValidationRule(
            x => x.Url,
            s => !string.IsNullOrWhiteSpace(s)
                 && Uri.TryCreate(s.Trim(), UriKind.Absolute, out var uri)
                 && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
            "Enter an absolute http(s) URL (e.g. https://api.example.com).");

        IsValid = this.WhenAnyValue(x => x.Url)
            .Select(_ => ValidationContext.IsValid)
            .StartWith(ValidationContext.IsValid);
    }

    public string TrimmedUrl => (Url ?? string.Empty).Trim().TrimEnd('/');

    public IObservable<bool> IsValid { get; }
}
