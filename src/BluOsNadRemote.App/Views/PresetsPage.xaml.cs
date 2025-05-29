namespace BluOsNadRemote.App.Views;

public partial class PresetsPage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private PresetsViewModel ViewModel => (PresetsViewModel)BindingContext;

    partial void PreConstruct() => InitializeComponent();
}
