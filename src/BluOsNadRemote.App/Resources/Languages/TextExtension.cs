namespace BluOsNadRemote.App.Resources.Languages;

[ContentProperty(nameof(Name))]
[AcceptEmptyServiceProvider]
public sealed class TextExtension : IMarkupExtension<BindingBase>
{
    public string Name { get; set; }

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
        =>  new Binding { 
            Mode = BindingMode.OneWay,
            Path = $"[{Name}]",
            Source = TextsViewModel.Instance
        };

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
