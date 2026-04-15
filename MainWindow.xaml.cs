using System.IO;
using Microsoft.Win32; // for OpenFileDialog / SaveFileDialog
using System.Drawing;
using System.Text;
using System.Diagnostics;
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
    private DateTime _currentMonthStart;
    private DateTime _currentMonthEnd;
    private IepYearFile _activeIepYear = new IepYearFile();
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
            _activeIepYear = welcomeWindow.getIepYearFile() ?? IepYearFileManager.CreateYear(IepYearFileManager.InferSchoolYear(_startDate), IepYearFileManager.BuildDefaultMonths(IepYearFileManager.InferSchoolYear(_startDate)));
            _studentFilePath = DataHandler.GetDataFilePath(welcomeWindow.getSaveFileName());
            _persistentStudentsPath = DataHandler.GetPersistentStudentsFilePath();

            // Load students using the new dual persistence model
            LoadStudentsWithIepData();

            PopulateIepMonthSelector();

            // Update the window title with the IEP filename
            string iepFileName = System.IO.Path.GetFileName(_studentFilePath);
            this.Title = $"TherapyTime - {iepFileName}";

            PopulateStudentMenus();
            CreateButtonGrid();
            ShowUpcomingReviewReminders();
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
        string iepJsonText = string.Empty;
        if (File.Exists(_persistentStudentsPath))
        {
            coreStudents = StudentManager.LoadCoreDataFromJson(File.ReadAllText(_persistentStudentsPath));
        }

        // Load IEP-specific data and year metadata
        var iepData = new List<StudentIepData>();
        if (File.Exists(_studentFilePath))
        {
            iepJsonText = File.ReadAllText(_studentFilePath);

            if (IepYearFileManager.TryLoadFromJson(iepJsonText, out IepYearFile? yearFile) && yearFile != null)
            {
                _activeIepYear = yearFile;
                _startDate = yearFile.SchoolYearStartDate;
                _endDate = yearFile.SchoolYearEndDate;
                iepData = yearFile.Students ?? new List<StudentIepData>();
            }
            else
            {
                iepData = StudentManager.LoadIepDataFromJson(iepJsonText);
            }
        }

        // Backward compatibility: older IEP files stored full Student objects.
        // If split-format parsing produced no rows, try legacy format and migrate.
        if (File.Exists(_studentFilePath) && iepData.Count == 0)
        {
            var legacyStudents = StudentManager.LoadFromJson(iepJsonText);
            if (legacyStudents.Count > 0)
            {
                _allStudents = legacyStudents;

                // Ensure core file exists and migrate this IEP to split format.
                SaveStudents();
                return;
            }
        }

        // Merge them
        _allStudents = StudentManager.MergeStudentData(coreStudents, iepData);

        // If core file doesn't exist yet, create it with current students
        if (!File.Exists(_persistentStudentsPath) && _allStudents.Count > 0)
        {
            SaveCoreStudentData();
        }
    }

    private void PopulateIepMonthSelector()
    {
        var months = _activeIepYear.Months
            .OrderBy(m => m.StartDate)
            .ToList();

        if (months.Count == 0)
        {
            months = IepYearFileManager.BuildDefaultMonths(_activeIepYear.SchoolYear == 0 ? IepYearFileManager.InferSchoolYear(DateTime.Today) : _activeIepYear.SchoolYear);
            _activeIepYear.Months = months;
            _startDate = months.Min(m => m.StartDate);
            _endDate = months.Max(m => m.EndDate);
        }

        IepMonthComboBox.ItemsSource = months;

        var todayMonth = months.FirstOrDefault(m => DateTime.Today.Date >= m.StartDate.Date && DateTime.Today.Date <= m.EndDate.Date);
        IepMonthComboBox.SelectedItem = todayMonth ?? months.First();
    }

    private void IepMonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IepMonthComboBox.SelectedItem is not IepMonthDefinition month)
            return;

        _currentMonthStart = month.StartDate.Date;
        _currentMonthEnd = month.EndDate.Date;
        CreateButtonGrid();
    }

    private void CreateButtonGrid()
    {
        panelGrid.Children.Clear();

        DateTime startDate = _currentMonthStart == default ? _startDate : _currentMonthStart;
        DateTime endDate = _currentMonthEnd == default ? _endDate : _currentMonthEnd;

        int totalDays = (endDate - startDate).Days + 1;
        if (totalDays < 1)
        {
            totalDays = dayMax;
            endDate = startDate.AddDays(dayMax - 1);
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
                var daySummary = GetDaySessionSummary(currentDay);
                int sessionCount = daySummary.Count;
                bool hasSessions = sessionCount > 0;

                var dayBorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
                var dayBorderThickness = new Thickness(1);
                string statusText = "No sessions";

                if (hasSessions)
                {
                    dayBorderThickness = new Thickness(3);

                    if (daySummary.AllCompleted)
                    {
                        dayBorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 125, 50));
                        statusText = "All completed";
                    }
                    else if (daySummary.NoneCompleted)
                    {
                        dayBorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 88, 58));
                        statusText = "None completed";
                    }
                    else
                    {
                        dayBorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(208, 148, 46));
                        statusText = "Mixed completion";
                    }
                }

                dayButton = new Button
                {
                    Content = hasSessions ? $"{currentDay.Day}\n({sessionCount})" : currentDay.Day.ToString(),
                    Tag = currentDay,
                    Width = 70,
                    Height = 70,
                    Margin = new Thickness(3),
                    Background = new SolidColorBrush(hasSessions
                        ? System.Windows.Media.Color.FromRgb(220, 245, 220)
                        : System.Windows.Media.Colors.White),
                    BorderBrush = dayBorderBrush,
                    BorderThickness = dayBorderThickness,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Cursor = Cursors.Hand
                };

                if (hasSessions)
                {
                    dayButton.ToolTip = $"{sessionCount} session(s) scheduled - {statusText}";
                }

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
                    else if (hasSessions)
                        dayButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 245, 220));
                    else
                        dayButton.Background = new SolidColorBrush(System.Windows.Media.Colors.White);
                };

                dayButton.Click += Button_Click;

                dayCounter++;
            }

            panelGrid.Children.Add(dayButton);
        }
    }

    private (int Count, bool AllCompleted, bool NoneCompleted) GetDaySessionSummary(DateTime date)
    {
        var sessionsOnDate = _allStudents
            .SelectMany(s => s.Sessions)
            .Where(sess => sess.Date.Date == date.Date)
            .ToList();

        if (sessionsOnDate.Count == 0)
        {
            return (0, false, false);
        }

        bool allCompleted = sessionsOnDate.All(s => s.IsCompleted);
        bool noneCompleted = sessionsOnDate.All(s => !s.IsCompleted);
        return (sessionsOnDate.Count, allCompleted, noneCompleted);
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
            CreateButtonGrid();
            MessageBox.Show($"Session added for {student.Name} on {date:MM/dd/yyyy} at {DateTime.Today.Add(sessionTime):hh:mm tt} ({minutes} minutes, code: {sessionCode}).", "Info");

            // Update the last session add date for future additions
            _lastSessionAddDate = date;
        }
    }

// --- New IEP Year ---
    private void NewIEP_Click(object sender, RoutedEventArgs e)
    {
        // Confirm with user
        if (MessageBox.Show("Open another IEP year? Unsaved changes should be saved first.", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            Welcome welcomeWindow = new Welcome();
            if (welcomeWindow.ShowDialog() == true)
            {
                _startDate = welcomeWindow.getStartDate();
                _endDate = welcomeWindow.getEndDate();
                _activeIepYear = welcomeWindow.getIepYearFile() ?? IepYearFileManager.CreateYear(IepYearFileManager.InferSchoolYear(_startDate), IepYearFileManager.BuildDefaultMonths(IepYearFileManager.InferSchoolYear(_startDate)));
                _studentFilePath = DataHandler.GetDataFilePath(welcomeWindow.getSaveFileName());
                _persistentStudentsPath = DataHandler.GetPersistentStudentsFilePath();

                // Load students using the new dual persistence model
                LoadStudentsWithIepData();
                PopulateIepMonthSelector();

                if (_allStudents.Count > 0)
                {
                    MessageBox.Show($"IEP year loaded with {_allStudents.Count} students.", "Info");
                }
                else
                {
                    MessageBox.Show("New IEP year created.", "Info");
                }

                // Update the window title with the IEP filename
                string iepFileName = System.IO.Path.GetFileName(_studentFilePath);
                this.Title = $"TherapyTime - {iepFileName}";

                PopulateStudentMenus();
                CreateButtonGrid();
                ShowUpcomingReviewReminders();
            }
        }
    }

    private void SaveIEP_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveStudents();
            MessageBox.Show("IEP year saved successfully.", "Info");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to save IEP year: " + ex.Message, "Error");
        }
    }

    // --- New Student ---
    private void NewStudent_Click(object sender, RoutedEventArgs e)
    {
        var addStudentWindow = new AddStudentWindow
        {
            Owner = this
        };

        if (addStudentWindow.ShowDialog() != true)
        {
            return;
        }

        Student newStudent = new Student(addStudentWindow.StudentName)
        {
            MonthlyRequiredMinutes = addStudentWindow.MonthlyRequiredMinutes,
            TotalMinutesRequired = addStudentWindow.MonthlyRequiredMinutes,
            FutureAnnualReviews = new List<DateTime> { addStudentWindow.FutureAnnualReview },
            NextThreeYearReevaluation = addStudentWindow.NextThreeYearReevaluation
        };

        if (addStudentWindow.PastAnnualReview.HasValue)
        {
            newStudent.PastAnnualReviews.Add(addStudentWindow.PastAnnualReview.Value);
        }

        _allStudents.Add(newStudent);
        SaveStudents();
        PopulateStudentMenus();
        MessageBox.Show($"Student '{addStudentWindow.StudentName}' added.", "Info");
    }

    private void PopulateStudentMenus()
    {
        UpdateRequiredMinutesMenu.Items.Clear();
        ArchiveStudentMenu.Items.Clear();
        UnarchiveStudentMenu.Items.Clear();
        DeleteStudentMenu.Items.Clear();

        var activeStudents = _allStudents.Where(s => !s.IsArchived).OrderBy(s => s.Name).ToList();
        var archivedStudents = _allStudents.Where(s => s.IsArchived).OrderBy(s => s.Name).ToList();
        var allStudentsSorted = _allStudents.OrderBy(s => s.Name).ToList();

        if (allStudentsSorted.Count == 0)
        {
            MenuItem updateEmptyItem = new MenuItem { Header = "(No students)" };
            updateEmptyItem.IsEnabled = false;
            UpdateRequiredMinutesMenu.Items.Add(updateEmptyItem);
        }
        else
        {
            foreach (var student in allStudentsSorted)
            {
                MenuItem updateMinutesItem = new MenuItem
                {
                    Header = student.IsArchived
                        ? $"{student.Name} (Archived) ({student.TotalMinutesRequired} min)"
                        : $"{student.Name} ({student.TotalMinutesRequired} min)",
                    Tag = student
                };
                updateMinutesItem.Click += UpdateRequiredMinutes_Click;
                UpdateRequiredMinutesMenu.Items.Add(updateMinutesItem);
            }
        }

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

    private void UpdateRequiredMinutes_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem || menuItem.Tag is not Student student)
            return;

        string? input = Microsoft.VisualBasic.Interaction.InputBox(
            $"Enter required minutes for {student.Name}:",
            "Update Required Minutes",
            student.TotalMinutesRequired.ToString());

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        if (!int.TryParse(input, out int newRequiredMinutes) || newRequiredMinutes < 0)
        {
            MessageBox.Show("Please enter a valid non-negative number of minutes.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        student.TotalMinutesRequired = newRequiredMinutes;
        SaveStudents();
        PopulateStudentMenus();
        MessageBox.Show($"Updated required minutes for {student.Name} to {newRequiredMinutes}.", "Updated", MessageBoxButton.OK, MessageBoxImage.Information);
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
                $"Deleting {student.Name} will permanently remove all their sessions in this IEP year range.\n\n" +
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

    private void HelpBubble_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Main Calendar Help\n\n" +
            "Purpose:\n" +
            "- Central workspace for IEP scheduling, editing, and statistics.\n\n" +
            "How to use this window:\n" +
            "- Use the Student menu to add students, sessions, and manage required minutes.\n" +
            "- Click any day button to edit sessions for that date.\n" +
            "- Use Statistics for progress and review-date reporting.\n\n" +
            "What day indicators mean:\n" +
            "- Green border: all sessions completed for that day.\n" +
            "- Amber border: mixed completion status.\n" +
            "- Red border: none completed.\n" +
            "- Day count in parentheses: number of sessions scheduled that day.",
            "Main Window Help",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Documentation_Click(object sender, RoutedEventArgs e)
    {
        const string documentationUrl = "https://github.com/TheBuilderHero/TherapyTime/blob/master/README";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = documentationUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Unable to open documentation link.\n\n" +
                documentationUrl + "\n\n" +
                "Error: " + ex.Message,
                "Open Documentation Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ReportIssue_Click(object sender, RoutedEventArgs e)
    {
        const string issueUrl = "https://github.com/TheBuilderHero/TherapyTime/issues";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = issueUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Unable to open issue tracker link.\n\n" +
                issueUrl + "\n\n" +
                "Error: " + ex.Message,
                "Open Issue Tracker Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ShowStudentStatistics_Click(object sender, RoutedEventArgs e)
    {
        if (_allStudents.Count == 0)
        {
            MessageBox.Show("No students available for statistics.", "Info");
            return;
        }

        var statisticsWindow = new StatisticsWindow(_allStudents, _startDate, _endDate)
        {
            Owner = this
        };

        statisticsWindow.ShowDialog();
    }

    private void ShowDailyView_Click(object sender, RoutedEventArgs e)
    {
        if (_allStudents.Count == 0)
        {
            MessageBox.Show("No students available for daily view.", "Info");
            return;
        }

        var dailyViewWindow = new DailyViewWindow(_allStudents, _startDate, _endDate)
        {
            Owner = this
        };

        dailyViewWindow.ShowDialog();
    }

    private void ShowReviewDates_Click(object sender, RoutedEventArgs e)
    {
        if (_allStudents.Count == 0)
        {
            MessageBox.Show("No students available for review dates.", "Info");
            return;
        }

        var reviewDatesWindow = new ReviewDatesWindow(_allStudents)
        {
            Owner = this
        };

        reviewDatesWindow.ShowDialog();
    }

    private void ShowUpcomingReviewReminders()
    {
        DateTime today = DateTime.Today;
        DateTime reminderEnd = today.AddDays(30);

        var annualReviewReminders = _allStudents
            .Where(student => !student.IsArchived)
            .SelectMany(student => student.FutureAnnualReviews
                .Where(date => date.Date >= today && date.Date <= reminderEnd)
                .Select(date => new { student.Name, Date = date.Date }))
            .OrderBy(item => item.Date)
            .ThenBy(item => item.Name)
            .ToList();

        var reevaluationReminders = _allStudents
            .Where(student => !student.IsArchived && student.NextThreeYearReevaluation.HasValue)
            .Select(student => new { student.Name, Date = student.NextThreeYearReevaluation!.Value.Date })
            .Where(item => item.Date >= today && item.Date <= reminderEnd)
            .OrderBy(item => item.Date)
            .ThenBy(item => item.Name)
            .ToList();

        if (annualReviewReminders.Count == 0 && reevaluationReminders.Count == 0)
        {
            return;
        }

        var reminderLines = new List<string>
        {
            $"Upcoming dates in the next 30 days ({today:MM/dd/yyyy} - {reminderEnd:MM/dd/yyyy}):",
            string.Empty
        };

        if (annualReviewReminders.Count > 0)
        {
            reminderLines.Add("Annual Reviews:");
            reminderLines.AddRange(annualReviewReminders.Select(item => $"- {item.Date:MM/dd/yyyy}: {item.Name}"));
            reminderLines.Add(string.Empty);
        }

        if (reevaluationReminders.Count > 0)
        {
            reminderLines.Add("3-Year Reevaluations:");
            reminderLines.AddRange(reevaluationReminders.Select(item => $"- {item.Date:MM/dd/yyyy}: {item.Name}"));
        }

        MessageBox.Show(string.Join(Environment.NewLine, reminderLines).TrimEnd(), "Upcoming Review Reminders", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SaveStudents()
    {
        // Save IEP-year metadata and IEP-specific student data in one file
        _activeIepYear.Students = _allStudents.Select(s => new StudentIepData
        {
            Id = s.Id,
            Sessions = s.Sessions,
            TotalMinutesReceived = s.TotalMinutesReceived,
            TotalMinutesRequired = s.TotalMinutesRequired
        }).ToList();

        if (_activeIepYear.Months.Count == 0)
        {
            _activeIepYear.Months = IepYearFileManager.BuildDefaultMonths(_activeIepYear.SchoolYear == 0 ? IepYearFileManager.InferSchoolYear(_startDate) : _activeIepYear.SchoolYear);
        }

        _activeIepYear.SchoolYearStartDate = _activeIepYear.Months.Min(m => m.StartDate).Date;
        _activeIepYear.SchoolYearEndDate = _activeIepYear.Months.Max(m => m.EndDate).Date;

        string iepYearJson = IepYearFileManager.SaveToJson(_activeIepYear);
        File.WriteAllText(_studentFilePath, iepYearJson);

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
                EditDaySessionsWindow editWindow = new EditDaySessionsWindow(_allStudents, activeStudents, day, _startDate, _endDate, SaveStudents)
                {
                    Owner = this
                };

                bool? result = editWindow.ShowDialog();

                if (result == true)
                {
                    SaveStudents();
                    CreateButtonGrid();
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