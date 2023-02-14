namespace BluOsNadRemote.App.Views;

public partial class AdvancedPage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private AdvancedViewModel ViewModel => BindingContext as AdvancedViewModel;

    partial void PreConstruct() => InitializeComponent();
}
