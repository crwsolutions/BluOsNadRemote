namespace BluOsNadRemote.App.Resources.Languages;

[ContentProperty(nameof(Name))]
public sealed class TextExtension : IMarkupExtension<BindingBase>
{
    public string Name { get; set; }

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Mode = BindingMode.OneWay,
            Path = $"[{Name}]",
            Source = Language.Instance
        };
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}
