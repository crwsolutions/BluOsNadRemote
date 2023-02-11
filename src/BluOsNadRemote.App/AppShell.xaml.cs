namespace BluOsNadRemote.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
        Routing.RegisterRoute(nameof(QueuePage), typeof(QueuePage));
    }
}
