using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App;

public partial class App : Application
{
    [Dependency]
    private readonly AdvancedViewModel _advancedViewModel;

    [Dependency]
    private readonly PlayerViewModel _playerViewModel;

    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    [Dependency]
    private readonly LanguageService _languageService;

    partial void PreConstruct()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }

    partial void PostConstruct()
    {
        _languageService.Initialize();
    }

    protected override void OnStart()
    {

    }

    protected override void OnSleep()
    {
        _playerViewModel.Dispose();
        _advancedViewModel.Dispose();
        _bluPlayerService.Disconnect();
    }

    protected override void OnResume()
    {
        _playerViewModel.IsBusy = true;
        _advancedViewModel.IsBusy = true;
    }
}
