namespace BluOsNadRemote.App.Views;

public partial class QueuePage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private QueueViewModel ViewModel => BindingContext as QueueViewModel;

    partial void PreConstruct() => InitializeComponent();
}
