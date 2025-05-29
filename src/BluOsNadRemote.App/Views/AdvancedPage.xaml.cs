namespace BluOsNadRemote.App.Views;

public partial class AdvancedPage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private AdvancedViewModel ViewModel => (AdvancedViewModel)BindingContext;

    partial void PreConstruct() => InitializeComponent();
}
