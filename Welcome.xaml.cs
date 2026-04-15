using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace TherapyTime;
public partial class Welcome : Window
{
    private DateTime _startDate;
    private DateTime _endDate;
    private string _saveFileName = string.Empty;
    private IepYearFile? _selectedIepYear;

    public Welcome()
    {
        InitializeComponent();

        int suggestedSchoolYear = IepYearFileManager.InferSchoolYear(DateTime.Today);
        SchoolYearTextBox.Text = suggestedSchoolYear.ToString();
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

            var jsonFiles = Directory.GetFiles(appFolderPath, "IEP_Year_*.json")
                .Where(f => !Path.GetFileName(f).Equals("students.json", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => File.GetLastWriteTime(f));

            if (!jsonFiles.Any())
            {
                jsonFiles = Directory.GetFiles(appFolderPath, "*.json")
                    .Where(f => !Path.GetFileName(f).Equals("students.json", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => File.GetLastWriteTime(f));
            }

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

        if (File.Exists(filePath) && IepYearFileManager.TryLoadFromJson(File.ReadAllText(filePath), out IepYearFile? yearFile) && yearFile != null)
        {
            _selectedIepYear = yearFile;
            _startDate = yearFile.SchoolYearStartDate;
            _endDate = yearFile.SchoolYearEndDate;
            _saveFileName = selectedFileName;
            this.DialogResult = true;
        }
        else if (TryParseLegacyIepFileName(Path.GetFileNameWithoutExtension(selectedFileName), out DateTime legacyStart, out DateTime legacyEnd))
        {
            int inferredYear = IepYearFileManager.InferSchoolYear(legacyStart);
            _selectedIepYear = IepYearFileManager.CreateYear(inferredYear, new List<IepMonthDefinition>
            {
                new IepMonthDefinition
                {
                    Name = $"Legacy Range ({legacyStart:MM/dd/yyyy}-{legacyEnd:MM/dd/yyyy})",
                    StartDate = legacyStart.Date,
                    EndDate = legacyEnd.Date
                }
            });

            _startDate = legacyStart.Date;
            _endDate = legacyEnd.Date;
            _saveFileName = selectedFileName;
            this.DialogResult = true;
        }
        else
        {
            MessageBox.Show("Unable to load the selected IEP year file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void IEPFileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        OpenSubmit_Click(null!, null!);
    }

    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(SchoolYearTextBox.Text, out int schoolYear) || schoolYear < 2000 || schoolYear > 3000)
        {
            MessageBox.Show("Enter a valid school year (example: 2025).", "Invalid School Year", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var setupWindow = new IepYearSetupWindow(schoolYear)
        {
            Owner = this
        };

        if (setupWindow.ShowDialog() != true || setupWindow.ConfiguredYear == null)
        {
            return;
        }

        _selectedIepYear = setupWindow.ConfiguredYear;
        _startDate = _selectedIepYear.SchoolYearStartDate;
        _endDate = _selectedIepYear.SchoolYearEndDate;
        _saveFileName = IepYearFileManager.GetFileName(_selectedIepYear.SchoolYear);

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

    public string getSaveFileName()
    {
        if (!string.IsNullOrEmpty(_saveFileName))
            return _saveFileName;

        int schoolYear = _selectedIepYear?.SchoolYear ?? IepYearFileManager.InferSchoolYear(_startDate == default ? DateTime.Today : _startDate);
        return IepYearFileManager.GetFileName(schoolYear);
    }

    public IepYearFile? getIepYearFile()
    {
        return _selectedIepYear;
    }

    private static bool TryParseLegacyIepFileName(string fileNameWithoutExtension, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;

        var parts = fileNameWithoutExtension.Split('_');
        if (parts.Length != 2)
            return false;

        if (!DateTime.TryParse(parts[0], out start) || !DateTime.TryParse(parts[1], out end))
            return false;

        return end.Date >= start.Date;
    }

    public void setStartDate(DateTime startDate)
    {
        _startDate = startDate;
    }

    private void HelpBubble_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Welcome Window Help\n\n" +
            "Purpose:\n" +
            "- Create a new IEP year or open an existing IEP year file.\n\n" +
            "How to use this window:\n" +
            "- Create New IEP Year: enter the school year, then configure all 12 IEP month date ranges.\n" +
            "- Open Existing IEP Year: select a file and click Open (or double-click).\n\n" +
            "What it means:\n" +
            "- Year files are saved as IEP_Year_####.json.\n" +
            "- The file stores the 12 IEP month ranges and that year's IEP session data.",
            "Welcome Help",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
