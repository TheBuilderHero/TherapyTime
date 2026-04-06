using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media.Imaging;

namespace TherapyTime;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	private static readonly Uri WindowIconUri = new("pack://application:,,,/TherapyTime;component/time-tracking.ico", UriKind.Absolute);

	protected override void OnStartup(StartupEventArgs e)
	{
		// Ensure every Window gets the app icon even if a local style overrides the global one.
		EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
		base.OnStartup(e);
	}

	private static void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is Window window && window.Icon is null)
		{
			window.Icon = BitmapFrame.Create(WindowIconUri);
		}
	}
}

