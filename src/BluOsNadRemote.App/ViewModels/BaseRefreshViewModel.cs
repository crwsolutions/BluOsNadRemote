using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public partial class BaseRefreshViewModel : BaseViewModel
{
    [Dependency]
    protected readonly NoConnectionDialogService _noConnectionDialogService;

    [ObservableProperty]
    public partial bool IsBusy { get; set; } = false;

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
    public partial string Title { get; set; }
}
