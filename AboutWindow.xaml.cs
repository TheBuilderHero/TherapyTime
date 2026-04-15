using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace TherapyTime;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = GetAppVersion();
    }

    private static string GetAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        string? productVersion = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;

        if (!string.IsNullOrWhiteSpace(productVersion))
        {
            int plusIndex = productVersion.IndexOf('+');
            return plusIndex > 0 ? productVersion[..plusIndex] : productVersion;
        }

        return assembly.GetName().Version?.ToString() ?? "Unknown";
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
