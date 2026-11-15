using Microsoft.Maui.Controls.Xaml;

namespace Player.App.Localization;

/// <summary>
/// XAML markup extension: <c>Text="{loc:T Explorer.Sort}"</c>. It returns a one-way binding to the observable text
/// of the key, so the text follows a language change without navigation.
/// </summary>
[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public sealed class TExtension : IMarkupExtension<BindingBase>
{
    /// <summary>The text key, for example "Explorer.Sort".</summary>
    public string Key { get; set; } = string.Empty;

    /// <inheritdoc />
    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        var text = Localizer.GetText(Key);
        return BindingBase.Create(static (LocalizedString s) => s.Value, BindingMode.OneWay, source: text);
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
