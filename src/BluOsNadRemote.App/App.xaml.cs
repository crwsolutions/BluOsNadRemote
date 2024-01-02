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

    partial void PreConstruct() 
    {
        InitializeComponent(); 
		MainPage = new AppShell();
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
