namespace BluOsNadRemote.App.Views;

public partial class QueuePage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private QueueViewModel ViewModel => (QueueViewModel)BindingContext;

    partial void PreConstruct() => InitializeComponent();
}
