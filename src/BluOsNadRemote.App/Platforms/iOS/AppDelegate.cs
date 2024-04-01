using Foundation;
using Microsoft.Maui.Handlers;

namespace BluOsNadRemote.App;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp()
    {
        var app = MauiProgram.CreateMauiApp();

        //Hack: remove cancel button on iOs that does nothing https://github.com/dotnet/maui/issues/13720
        SearchBarHandler.Mapper.AppendToMapping("CancelButtonColor", (searchBarHandler, searchBar) =>
        {
            searchBarHandler.PlatformView.SetShowsCancelButton(false, false);
        });

        return app;
    }
}
