using System.IO;
using Microsoft.Win32; // for OpenFileDialog / SaveFileDialog
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TherapyTime;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private int dayMax = 30; // fallback total number of days when end date is missing
    private DateTime _startDate;
    private DateTime _endDate;
    private string _studentFilePath = string.Empty;
    private string _persistentStudentsPath = string.Empty;
    private List<Student> _allStudents = new List<Student>();
    private DateTime? _lastSessionAddDate = null; // tracks the last date used for adding sessions
    public MainWindow()
    {
        InitializeComponent();

        Welcome welcomeWindow = new Welcome();
        if (welcomeWindow.ShowDialog() == true)
        {
            _startDate = welcomeWindow.getStartDate();
            _endDate = welcomeWindow.getEndDate();
            _studentFilePath = DataHandler.GetDataFilePath(welcomeWindow.getSaveFileName());
            _persistentStudentsPath = DataHandler.GetPersistentStudentsFilePath();

            // Load students using the new dual persistence model
            LoadStudentsWithIepData();

            // Update the window title with the IEP filename
            string iepFileName = System.IO.Path.GetFileName(_studentFilePath);
            this.Title = $"TherapyTime - {iepFileName}";

            PopulateStudentMenus();
            CreateButtonGrid();
        }
        else
        {
            Close();
        }
    }

    private void LoadStudentsWithIepData()
    {
        // Load core student data
        var coreStudents = new List<StudentCoreData>();
        if (File.Exists(_persistentStudentsPath))
        {
            coreStudents = StudentManager.LoadCoreDataFromJson(File.ReadAllText(_persistentStudentsPath));
        }

        // Load IEP-specific data
        var iepData = new List<StudentIepData>();
        if (File.Exists(_studentFilePath))
        {
            iepData = StudentManager.LoadIepDataFromJson(File.ReadAllText(_studentFilePath));
        }

        // Merge them
        _allStudents = StudentManager.MergeStudentData(coreStudents, iepData);

        // If core file doesn't exist yet, create it with current students
        if (!File.Exists(_persistentStudentsPath) && _allStudents.Count > 0)
        {
            SaveCoreStudentData();
        }
    }

    private void CreateButtonGrid()
    {
        panelGrid.Children.Clear();

        DateTime startDate = _startDate;
        int totalDays = (_endDate - startDate).Days + 1;
        if (totalDays < 1)
        {
            totalDays = dayMax;
            _endDate = startDate.AddDays(dayMax - 1);
        }

        // Calculate what day of the week the start date is (0=Sunday)
        int startDayOfWeek = (int)startDate.DayOfWeek;

        int rows = (int)Math.Ceiling((startDayOfWeek + totalDays) / 7.0);
        panelGrid.Rows = rows;
        panelGrid.Columns = 7;

        int dayCounter = 1;

        // Loop through each cell in the grid
        for (int cell = 0; cell < rows * 7; cell++)
        {
            Button dayButton;

            if (cell < startDayOfWeek || dayCounter > totalDays)
            {
                // Empty space before the start day or after the last day
                dayButton = new Button
                {
                    IsEnabled = false,
                    Background = new SolidColorBrush(System.Windows.Media.Colors.Transparent),
                    BorderThickness = new Thickness(0)
                };
            }
            else
            {
                DateTime currentDay = startDate.AddDays(dayCounter - 1);

                dayButton = new Button
                {
                    Content = currentDay.Day.ToString(),
                    Tag = currentDay,
                    Width = 70,
                    Height = 70,
                    Margin = new Thickness(3),
                    Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(1),
                    FontWeight = FontWeights.SemiBold,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Cursor = Cursors.Hand
                };

                // Highlight today
                if (currentDay.Date == DateTime.Today)
                {
                    dayButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 220, 255));
                }

                // Hover effect
                dayButton.MouseEnter += (s, e) =>
                {
                    dayButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 240, 255));
                };
                dayButton.MouseLeave += (s, e) =>
                {
                    if (currentDay.Date == DateTime.Today)
                        dayButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 220, 255));
                    else
                        dayButton.Background = new SolidColorBrush(System.Windows.Media.Colors.White);
                };

                dayButton.Click += Button_Click;

                dayCounter++;
            }

            panelGrid.Children.Add(dayButton);
        }
    }

    private void AddSession_Click(object sender, RoutedEventArgs e)
    {
        var activeStudents = _allStudents.Where(s => !s.IsArchived).ToList();
        if (activeStudents.Count == 0)
        {
            MessageBox.Show("No active students available to add a session.", "Info");
            return;
        }

        // Open AddSessionWindow with all students in dropdown
        // Use last session add date if available, otherwise use today
        DateTime defaultDate = _lastSessionAddDate ?? DateTime.Today;
        AddSessionWindow addSessionWindow = new AddSessionWindow(activeStudents, _startDate, _endDate, defaultDate, false)
        {
            Owner = this
        };

        if (addSessionWindow.ShowDialog() == true)
        {
            var student = addSessionWindow.SelectedStudent!;
            var date = addSessionWindow.SelectedDate;
            int minutes = addSessionWindow.Minutes;
            SessionCode sessionCode = addSessionWindow.SessionCode;
            TimeSpan sessionTime = addSessionWindow.SessionTime;

            // Check for overlapping sessions across all students on the selected date
            var newStart = sessionTime;
            var newEnd = newStart.Add(TimeSpan.FromMinutes(minutes));
            var conflict = _allStudents
                .SelectMany(s => s.Sessions
                    .Where(sess => sess.Date.Date == date.Date)
                    .Select(sess => new { Student = s, Session = sess }))
                .FirstOrDefault(x =>
                {
                    var existingEnd = x.Session.TimeOfDay.Add(TimeSpan.FromMinutes(x.Session.Minutes));
                    return newStart < existingEnd && newEnd > x.Session.TimeOfDay;
                });
            if (conflict != null)
            {
                MessageBox.Show(
                    $"This time conflicts with an existing session for {conflict.Student.Name}:\n" +
                    $"{DateTime.Today.Add(conflict.Session.TimeOfDay):hh:mm tt} - {conflict.Session.Minutes} min ({conflict.Session.Code})",
                    "Session Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            student.ScheduleSession(date, minutes, sessionCode, sessionTime);
            SaveStudents();
            MessageBox.Show($"Session added for {student.Name} on {date:MM/dd/yyyy} at {DateTime.Today.Add(sessionTime):hh:mm tt} ({minutes} minutes, code: {sessionCode}).", "Info");

            // Update the last session add date for future additions
            _lastSessionAddDate = date;
        }
    }

// --- New IEP ---
    private void NewIEP_Click(object sender, RoutedEventArgs e)
    {
        // Confirm with user
        if (MessageBox.Show("Start a new IEP? This will clear current data and return to the Welcome screen.", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            Welcome welcomeWindow = new Welcome();
            if (welcomeWindow.ShowDialog() == true)
            {
                _startDate = welcomeWindow.getStartDate();
                _endDate = welcomeWindow.getEndDate();
                _studentFilePath = DataHandler.GetDataFilePath(welcomeWindow.getSaveFileName());
                _persistentStudentsPath = DataHandler.GetPersistentStudentsFilePath();

                // Load students using the new dual persistence model
                LoadStudentsWithIepData();

                if (_allStudents.Count > 0)
                {
                    MessageBox.Show($"IEP loaded with {_allStudents.Count} students.", "Info");
                }
                else
                {
                    MessageBox.Show("New IEP created.", "Info");
                }

                // Update the window title with the IEP filename
                string iepFileName = System.IO.Path.GetFileName(_studentFilePath);
                this.Title = $"TherapyTime - {iepFileName}";

                PopulateStudentMenus();
                CreateButtonGrid();
            }
        }
    }

    private void SaveIEP_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveStudents();
            MessageBox.Show("IEP saved successfully.", "Info");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to save IEP: " + ex.Message, "Error");
        }
    }

    // --- New Student ---
    private void NewStudent_Click(object sender, RoutedEventArgs e)
    {
        string? studentName = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter the new student's name:",
            "New Student",
            "");

        if (!string.IsNullOrWhiteSpace(studentName))
        {
            Student newStudent = new Student(studentName);
            _allStudents.Add(newStudent);
            SaveStudents();
            // After adding a new student
            PopulateStudentMenus();
            MessageBox.Show($"Student '{studentName}' added.", "Info");
        }
        else
        {
            MessageBox.Show("No name entered. Student not added.", "Info");
        }
    }

    private void PopulateStudentMenus()
    {
        ArchiveStudentMenu.Items.Clear();
        UnarchiveStudentMenu.Items.Clear();
        DeleteStudentMenu.Items.Clear();

        var activeStudents = _allStudents.Where(s => !s.IsArchived).OrderBy(s => s.Name).ToList();
        var archivedStudents = _allStudents.Where(s => s.IsArchived).OrderBy(s => s.Name).ToList();
        var allStudentsSorted = _allStudents.OrderBy(s => s.Name).ToList();

        if (allStudentsSorted.Count == 0)
        {
            MenuItem deleteEmptyItem = new MenuItem { Header = "(No students)" };
            deleteEmptyItem.IsEnabled = false;
            DeleteStudentMenu.Items.Add(deleteEmptyItem);
        }

        if (activeStudents.Count == 0)
        {
            MenuItem emptyItem = new MenuItem { Header = "(No students)" };
            emptyItem.IsEnabled = false;
            ArchiveStudentMenu.Items.Add(emptyItem);
        }
        else
        {
            foreach (var student in activeStudents)
            {
                MenuItem studentItem = new MenuItem
                {
                    Header = student.Name,
                    Tag = student
                };
                studentItem.Click += ArchiveStudent_Click;
                ArchiveStudentMenu.Items.Add(studentItem);
            }
        }

        if (allStudentsSorted.Count > 0)
        {
            foreach (var student in allStudentsSorted)
            {
                MenuItem deleteStudentItem = new MenuItem
                {
                    Header = student.IsArchived ? $"{student.Name} (Archived)" : student.Name,
                    Tag = student
                };
                deleteStudentItem.Click += DeleteStudent_Click;
                DeleteStudentMenu.Items.Add(deleteStudentItem);
            }
        }

        if (archivedStudents.Count == 0)
        {
            MenuItem emptyItem = new MenuItem { Header = "(No archived students)" };
            emptyItem.IsEnabled = false;
            UnarchiveStudentMenu.Items.Add(emptyItem);
        }
        else
        {
            foreach (var student in archivedStudents)
            {
                MenuItem studentItem = new MenuItem
                {
                    Header = student.Name,
                    Tag = student
                };
                studentItem.Click += UnarchiveStudent_Click;
                UnarchiveStudentMenu.Items.Add(studentItem);
            }
        }
    }

    private void DeleteStudent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem || menuItem.Tag is not Student student)
            return;

        var sessionsInIepRange = student.Sessions
            .Where(s => s.Date.Date >= _startDate.Date && s.Date.Date <= _endDate.Date)
            .OrderBy(s => s.SessionDateTime)
            .ToList();

        if (sessionsInIepRange.Count > 0)
        {
            string sessionList = string.Join("\n", sessionsInIepRange.Select(s =>
                $"- {s.Date:MM/dd/yyyy} {DateTime.Today.Add(s.TimeOfDay):hh:mm tt} ({s.Minutes} min, {s.Code})"));

            var warningResult = MessageBox.Show(
                $"Deleting {student.Name} will permanently remove all their sessions in this IEP range.\n\n" +
                "Sessions that will be deleted:\n" +
                $"{sessionList}\n\n" +
                "Do you want to continue?",
                "Delete Student Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (warningResult != MessageBoxResult.Yes)
                return;
        }
        else
        {
            var confirmNoSessions = MessageBox.Show(
                $"Delete {student.Name}? This cannot be undone.",
                "Delete Student",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmNoSessions != MessageBoxResult.Yes)
                return;
        }

        var finalConfirm = MessageBox.Show(
            $"Final confirmation: delete {student.Name}?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Stop);

        if (finalConfirm != MessageBoxResult.Yes)
            return;

        _allStudents.Remove(student);
        SaveStudents();
        PopulateStudentMenus();
        CreateButtonGrid();
        MessageBox.Show($"{student.Name} has been deleted.", "Delete Complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ArchiveStudent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is Student student)
        {
            var result = MessageBox.Show(
                $"Archive {student.Name}?\n\nArchived students are hidden from all Add Session flows, but existing data is preserved.",
                "Archive Student",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            student.IsArchived = true;
            SaveStudents();
            PopulateStudentMenus();
            MessageBox.Show($"{student.Name} has been archived.", "Archive Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void UnarchiveStudent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is Student student)
        {
            var result = MessageBox.Show(
                $"Unarchive {student.Name}?\n\nThey will appear again in all Add Session lists.",
                "Unarchive Student",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            student.IsArchived = false;
            SaveStudents();
            PopulateStudentMenus();
            MessageBox.Show($"{student.Name} has been unarchived.", "Unarchive Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }



    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("TherapyTime v1.0", "About");
    }

    private void SaveStudents()
    {
        // Save IEP-specific data to the IEP file
        string iepJson = StudentManager.SaveIepDataToJson(_allStudents);
        File.WriteAllText(_studentFilePath, iepJson);

        // Save core student data to persistent file
        SaveCoreStudentData();
    }

    private void SaveCoreStudentData()
    {
        string coreJson = StudentManager.SaveCoreDataToJson(_allStudents);
        File.WriteAllText(_persistentStudentsPath, coreJson);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button clickedButton && clickedButton.Tag is DateTime day)
        {
            if (_allStudents.Count == 0)
            {
                MessageBox.Show("No students available. Add a student first.", "Info");
                return;
            }

            try
            {
                var activeStudents = _allStudents.Where(s => !s.IsArchived).ToList();
                EditDaySessionsWindow editWindow = new EditDaySessionsWindow(_allStudents, activeStudents, day, _startDate, _endDate)
                {
                    Owner = this
                };

                bool? result = editWindow.ShowDialog();

                if (result == true)
                {
                    SaveStudents();
                    MessageBox.Show($"Sessions for {day:MM/dd/yyyy} saved.", "Info");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening session editor: " + ex.Message, "Error");
            }
        }
    }

}