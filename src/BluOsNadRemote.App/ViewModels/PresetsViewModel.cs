using BluOsNadRemote.App.Extensions;
using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public partial class PresetsViewModel : BaseRefreshViewModel
{
    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    [ObservableProperty]
    public IEnumerable<Favorite> _presets;

    [RelayCommand]
    private async Task PresetTappedAsync(Favorite preset)
    {
        await _bluPlayerService.BluPlayer.PresetList.LoadPreset(preset.Number);
        await Shell.Current.GoToAsync($"//{nameof(PlayerPage)}");
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            Title = AppResources.Loading;

            var result = await _bluPlayerService.ConnectAsync();
            Title = result.Message;
            if (result.IsConnected == false)
            {
                return;
            }

            var presets = await _bluPlayerService.BluPlayer.PresetList.GetPresets();
            Presets = presets.Select(preset => new Favorite(preset));
            Title = AppResources.NumPresets.Interpolate(presets.Count);
        }
        catch (Exception exception)
        {
            Title = AppResources.NoPresets;
            Debug.WriteLine(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }
}