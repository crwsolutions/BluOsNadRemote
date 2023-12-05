namespace BluOsNadRemote.App.ContentViews;

public partial class SinglelineMusicContentEntry : ContentView
{
    public static readonly BindableProperty ValueProperty =
         BindableProperty.Create(
            propertyName: nameof(Value),
             returnType: typeof(MusicContentEntryViewModel),
             declaringType: typeof(SinglelineMusicContentEntry),
             defaultValue: null,
             defaultBindingMode: BindingMode.TwoWay);

    public SinglelineMusicContentEntry()
	{
		InitializeComponent();
	}

    public MusicContentEntryViewModel Value
    {
        get { return (MusicContentEntryViewModel)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    private async void More_Clicked(object sender, EventArgs e)
    {
        var btn = (BindableObject)sender;
        var entry = (MusicContentEntryViewModel)btn.BindingContext;

        Debug.WriteLine(entry.Entry.Name);

        var contextMenu = await entry.Entry.ResolveContextMenu();

        Debug.WriteLine(contextMenu.Entries.Count);
        var options = contextMenu.Entries.Select(t => t.Name).ToArray();
        
        var page = Shell.Current.CurrentPage as BrowsePage;

        string action = await page.DisplayActionSheet("Actions", "Cancel", null, options);

        var actionEntry = contextMenu.Entries.FirstOrDefault(e => e.Name == action);

        Debug.WriteLine("Action clicked: " + actionEntry?.ActionURL);

        if (actionEntry == null)
        {
            return;
        }

        page.ViewModel.ActionURL = actionEntry.ActionURL;
        page.ViewModel.IsBusy = true;
    }
}