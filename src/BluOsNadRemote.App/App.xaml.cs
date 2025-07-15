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

    [Dependency]
    private readonly ThemeService _themeService;

    [Dependency]
    public IServiceProvider ServiceProvider { get; }

    partial void PostConstruct()
    {
        InitializeComponent();
        _languageService.Initialize();
        _themeService.Initialize();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
#if WINDOWS
    const int newHeight = 900;
    const int newWidth = 450;
    var density = DeviceDisplay.MainDisplayInfo.Density;
    var screenWidth = DeviceDisplay.MainDisplayInfo.Width / density;
    var screenHeight = DeviceDisplay.MainDisplayInfo.Height / density;

    return new Window(new AppShell())
    {
        Height = newHeight,
        Width = newWidth,
        X = (screenWidth / 2) - (newWidth / 2),
        Y = (screenHeight / 2) - (newHeight / 2)
    };
#else
        return new(new AppShell());
#endif
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
