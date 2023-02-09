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
        Title = "Loading...";

        try
        {
            if (!_bluPlayerService.IsInitialized) { 
                Title = await _bluPlayerService.InitializeAsync();
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