using BluOsNadRemote.App.Resources.Languages;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace BluOsNadRemote.App.Services;

public class NoConnectionDialogService
{
    public async Task ShowAsync() => await Snackbar.Make(
            AppResources.NoConnectionDialogMessage,
            null,
            AppResources.Ok,
            TimeSpan.FromSeconds(7),
            new SnackbarOptions
            {
                BackgroundColor = Colors.DarkOrange,
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.White,
                CornerRadius = 8,
                Font = Microsoft.Maui.Font.SystemFontOfSize(14),

            }).Show();
}
