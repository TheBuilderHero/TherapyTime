using System.Collections.ObjectModel;
using System.Windows;

namespace TherapyTime;

public partial class IepYearSetupWindow : Window
{
    private ObservableCollection<IepMonthDefinition> _months = new ObservableCollection<IepMonthDefinition>();

    public IepYearFile? ConfiguredYear { get; private set; }

    public IepYearSetupWindow(int suggestedSchoolYear)
    {
        InitializeComponent();

        SchoolYearTextBox.Text = suggestedSchoolYear.ToString();
        LoadDefaultMonths(suggestedSchoolYear);
    }

    private void LoadDefaults_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(SchoolYearTextBox.Text, out int schoolYear) || schoolYear < 2000 || schoolYear > 3000)
        {
            MessageBox.Show("Enter a valid school year (example: 2025).", "Invalid School Year", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LoadDefaultMonths(schoolYear);
    }

    private void LoadDefaultMonths(int schoolYear)
    {
        _months = new ObservableCollection<IepMonthDefinition>(IepYearFileManager.BuildDefaultMonths(schoolYear));
        MonthsGrid.ItemsSource = _months;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(SchoolYearTextBox.Text, out int schoolYear) || schoolYear < 2000 || schoolYear > 3000)
        {
            MessageBox.Show("Enter a valid school year (example: 2025).", "Invalid School Year", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var months = _months.Select(m => new IepMonthDefinition
        {
            Name = m.Name,
            StartDate = m.StartDate.Date,
            EndDate = m.EndDate.Date
        }).ToList();

        if (!IepYearFileManager.ValidateMonths(months, out string error))
        {
            MessageBox.Show(error, "Invalid IEP Month Setup", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ConfiguredYear = IepYearFileManager.CreateYear(schoolYear, months);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
