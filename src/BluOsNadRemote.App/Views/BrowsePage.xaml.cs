namespace BluOsNadRemote.App.Views;

public partial class BrowsePage : ContentPage
{
	public BrowsePage(BrowseViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
