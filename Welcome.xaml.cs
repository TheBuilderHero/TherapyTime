using System.Collections;
using System.Windows;

namespace TherapyTime;
public partial class Welcome : Window
{
    private DateTime _startDate;
    public Welcome()
    {
        InitializeComponent(); // This "sews" the XAML and C# together
    }

    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        // Now you can use IEPStartDate directly!
        _startDate = IEPStartDate.SelectedDate ?? DateTime.Now;
        string formattedDate = _startDate.ToShortDateString();
        MessageBox.Show($"Started on: {formattedDate}");
        this.DialogResult = true;
    }

    public DateTime getStartDate()
    {
        return _startDate;
    }

    public void setStartDate(DateTime startDate)
    {
        _startDate = startDate;
    }
}
