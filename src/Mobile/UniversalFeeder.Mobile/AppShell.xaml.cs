namespace UniversalFeeder.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

			Routing.RegisterRoute("SchedulePage", typeof(Views.SchedulePage));
			Routing.RegisterRoute("FeedTypePage", typeof(Views.FeedTypePage));
	}
}
