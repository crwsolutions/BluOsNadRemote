namespace BluOsNadRemote.App.Views;

public partial class AdvancedPage : ContentPage
{
	public AdvancedPage(AdvancedViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
