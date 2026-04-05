using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace TherapyTime;
public partial class Welcome : Window
{
    private DateTime _startDate;
    private DateTime _endDate;
    private string _saveFileName = string.Empty;
    private const int DefaultIepDays = 30;

    public Welcome()
    {
        InitializeComponent(); // This "sews" the XAML and C# together
        IEPStartDate.SelectedDate = DateTime.Today;
        IEPEndDate.SelectedDate = DateTime.Today.AddDays(DefaultIepDays - 1);
        UpdateDefaultFileName();
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        NewPanel.Visibility = Visibility.Visible;
        OpenPanel.Visibility = Visibility.Collapsed;
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        NewPanel.Visibility = Visibility.Collapsed;
        OpenPanel.Visibility = Visibility.Visible;
        PopulateIEPFileList();
    }

    private void PopulateIEPFileList()
    {
        try
        {
            IEPFileList.Items.Clear();
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolderPath = Path.Combine(appDataPath, "TherapyTime");

            if (!Directory.Exists(appFolderPath))
            {
                MessageBox.Show("No IEPs found.", "Info");
                return;
            }

            var jsonFiles = Directory.GetFiles(appFolderPath, "*.json")
                .Where(f => !Path.GetFileName(f).Equals("students.json", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => File.GetLastWriteTime(f));

            foreach (var filePath in jsonFiles)
            {
                string fileName = Path.GetFileName(filePath);
                IEPFileList.Items.Add(fileName);
            }

            if (IEPFileList.Items.Count == 0)
            {
                MessageBox.Show("No IEPs found.", "Info");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading IEP files: " + ex.Message, "Error");
        }
    }

    private void OpenSubmit_Click(object sender, RoutedEventArgs? e)
    {
        if (IEPFileList.SelectedItem == null)
        {
            MessageBox.Show("Please select an IEP file.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string selectedFileName = (IEPFileList.SelectedItem.ToString()) ?? "";
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolderPath = Path.Combine(appDataPath, "TherapyTime");
        string filePath = Path.Combine(appFolderPath, selectedFileName);

        // Extract dates from filename to populate _startDate and _endDate
        if (TryParseIEPFileName(Path.GetFileNameWithoutExtension(selectedFileName), out DateTime start, out DateTime end))
        {
            _startDate = start;
            _endDate = end;
            _saveFileName = selectedFileName;
            this.DialogResult = true;
        }
        else
        {
            MessageBox.Show("Unable to parse the IEP file name. Please use the correct format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void IEPFileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        OpenSubmit_Click(null!, null!);
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

        // Check for overlapping IEPs
        if (CheckForOverlappingIEPs(_startDate, _endDate))
        {
            MessageBoxResult result = MessageBox.Show(
                "The selected date range overlaps with an existing IEP. This may cause data conflicts or confusion.\n\nAre you absolutely sure you want to proceed?",
                "Overlapping IEP Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        _saveFileName = GetDefaultFileName(_startDate, _endDate);

        this.DialogResult = true;
    }

    private static bool CheckForOverlappingIEPs(DateTime newStart, DateTime newEnd)
    {
        try
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolderPath = Path.Combine(appDataPath, "TherapyTime");

            if (!Directory.Exists(appFolderPath))
                return false;

            var jsonFiles = Directory.GetFiles(appFolderPath, "*.json")
                .Where(f => !Path.GetFileName(f).Equals("students.json", StringComparison.OrdinalIgnoreCase));

            foreach (var file in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                
                // Try to parse the filename as a date range (yyyy-MM-dd_yyyy-MM-dd)
                if (TryParseIEPFileName(fileName, out DateTime existingStart, out DateTime existingEnd))
                {
                    // Check if ranges overlap
                    if (newStart <= existingEnd && newEnd >= existingStart)
                    {
                        return true; // Overlap detected
                    }
                }
            }

            return false; // No overlaps found
        }
        catch
        {
            return false; // If any error, allow the operation
        }
    }

    private static bool TryParseIEPFileName(string fileName, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;

        var parts = fileName.Split('_');
        if (parts.Length != 2)
            return false;

        if (DateTime.TryParseExact(parts[0], "yyyy-MM-dd", null, DateTimeStyles.None, out start) &&
            DateTime.TryParseExact(parts[1], "yyyy-MM-dd", null, DateTimeStyles.None, out end))
        {
            return true;
        }

        return false;
    }

    private void IEPStartDate_SelectedDateChanged(object sender, RoutedEventArgs e)
    {
        // When start date changes, automatically set end date to 30 days later
        DateTime? startDate = IEPStartDate.SelectedDate;
        if (startDate.HasValue)
        {
            IEPEndDate.SelectedDate = startDate.Value.AddDays(DefaultIepDays - 1);
        }

        UpdateDefaultFileName();
    }

    private void IEPEndDate_SelectedDateChanged(object sender, RoutedEventArgs e)
    {
        UpdateDefaultFileName();
    }

    private void UpdateDefaultFileName()
    {
        _startDate = IEPStartDate.SelectedDate ?? DateTime.Today;
        _endDate = IEPEndDate.SelectedDate ?? _startDate.AddDays(DefaultIepDays - 1);
        // filename is auto-generated from dates; no textbox to update
    }

    private static string GetDefaultFileName(DateTime start, DateTime end)
    {
        return $"{start:yyyy-MM-dd}_{end:yyyy-MM-dd}.json";
    }

    private static string NormalizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            name += ".json";

        return name;
    }

    public DateTime getStartDate()
    {
        return _startDate;
    }

    public DateTime getEndDate()
    {
        return _endDate;
    }

    public string getSaveFileName()
    {
        if (!string.IsNullOrEmpty(_saveFileName))
            return _saveFileName;

        return GetDefaultFileName(_startDate, _endDate);
    }

    public void setStartDate(DateTime startDate)
    {
        _startDate = startDate;
    }
}
