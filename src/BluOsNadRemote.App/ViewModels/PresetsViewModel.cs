using BluOsNadRemote.App.Models;
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
            Title = "Loading...";

            if (!_bluPlayerService.IsConnected) {
                await Shell.Current.GoToAsync($"{nameof(SettingsPage)}");
            }

            var presets = await _bluPlayerService.BluPlayer.PresetList.GetPresets();
            Presets = presets.Select(preset => new Favorite(preset));
            Title = $"{presets.Count} Presets";
        }
        catch (Exception exception)
        {
            Title = "Could not retrieve favorites";
            Debug.WriteLine(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }
}