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

    protected override void OnDisappearing()
    {
        var viewmodel = BindingContext as IDisposable;
        viewmodel?.Dispose();

        if (viewmodel != null)
        { 
            Debug.WriteLine("protected override void OnDisappearing");
        }

        base.OnDisappearing();
    }
}
