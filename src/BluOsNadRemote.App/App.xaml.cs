using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App;

public partial class App : Application
{
    [Dependency]
    private readonly AdvancedViewModel _advancedViewModel;

    [Dependency]
    private readonly PlayerViewModel _playerViewModel;

    [Dependency]
    private readonly PreferencesRepository _preferencesRepository;

    partial void PreConstruct() 
    {
        InitializeComponent(); 
		MainPage = new AppShell();
    }

    protected override void OnStart()
    {
        if (_preferencesRepository.SelectedEndpoint == null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Shell.Current.GoToAsync($"/{nameof(SettingsPage)}");
            });
        }
    }

    protected override void OnSleep()
    {
        _playerViewModel.Dispose();
        _advancedViewModel.Dispose();
    }

    protected override void OnResume()
    {
        _playerViewModel.IsBusy = true;
        _advancedViewModel.IsBusy = true;
    }
}
