namespace BluOsNadRemote.App.Views;

public partial class PresetsPage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private PresetsViewModel ViewModel => BindingContext as PresetsViewModel;

    partial void PreConstruct() => InitializeComponent();
}
