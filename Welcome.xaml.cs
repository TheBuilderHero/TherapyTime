using System;
using System.Collections;
using System.Windows;

namespace TherapyTime;
public partial class Welcome : Window
{
    private DateTime _startDate;
    private DateTime _endDate;
    private const int DefaultIepDays = 30;

    public Welcome()
    {
        InitializeComponent(); // This "sews" the XAML and C# together
        IEPStartDate.SelectedDate = DateTime.Today;
        IEPEndDate.SelectedDate = DateTime.Today.AddDays(DefaultIepDays - 1);
    }

    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        _startDate = IEPStartDate.SelectedDate ?? DateTime.Today;
        _endDate = IEPEndDate.SelectedDate ?? _startDate.AddDays(DefaultIepDays - 1);

        if (_endDate < _startDate)
        {
            MessageBox.Show("End date must be the same as or after the start date.", "Invalid Date Range", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        this.DialogResult = true;
    }

    public DateTime getStartDate()
    {
        return _startDate;
    }

    public DateTime getEndDate()
    {
        return _endDate;
    }

    public void setStartDate(DateTime startDate)
    {
        _startDate = startDate;
    }
}
