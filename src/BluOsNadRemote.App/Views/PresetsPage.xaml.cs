namespace BluOsNadRemote.App.Views;

public partial class PresetsPage : ContentPage
{
	public PresetsPage(PresetsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
