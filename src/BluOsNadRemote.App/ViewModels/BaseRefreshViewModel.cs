namespace BluOsNadRemote.App.ViewModels;

public partial class BaseRefreshViewModel : BaseViewModel
{
    [ObservableProperty]
    private bool _isBusy = false;

    partial void OnIsBusyChanged(bool oldValue, bool newValue)
    {
        if (!oldValue && newValue)
        {
            IsLoading();
        }
    }

    public virtual void IsLoading()
    { 
    }

    [ObservableProperty]
    private string _title;
}
