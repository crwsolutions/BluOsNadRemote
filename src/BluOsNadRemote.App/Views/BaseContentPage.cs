namespace BluOsNadRemote.App.Views;

public class BaseContentPage : ContentPage
{
    protected override void OnAppearing()
    {
        var viewmodel = BindingContext as BaseRefreshViewModel;

        if (viewmodel?.IsBusy == false)
        {
            Debug.WriteLine("protected override void OnAppearing");
            viewmodel.IsBusy = true;
        }

        base.OnAppearing();
    }

    protected override async void OnDisappearing()
    {
        if (BindingContext is IDisposable disposable)
        {
            Debug.WriteLine("protected override void OnDisappearing: Dispose()");
            disposable.Dispose();
        }

        if (BindingContext is IAsyncDisposable asyncDisposable)
        {
            Debug.WriteLine("protected override void OnDisappearing: DisposeAsync()");
            await asyncDisposable.DisposeAsync();
        }

        base.OnDisappearing();
    }
}
