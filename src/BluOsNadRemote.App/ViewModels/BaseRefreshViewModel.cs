namespace BluOsNadRemote.App.ViewModels;

public partial class BaseRefreshViewModel : BaseViewModel
{
    [ObservableProperty]
    private bool _isBusy = false;

    [ObservableProperty]
    private string _title;
}
