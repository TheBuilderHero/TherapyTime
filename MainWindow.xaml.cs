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
    private List<Student> _allStudents = new List<Student>();
    public MainWindow()
    {
        InitializeComponent();

        Welcome welcomeWindow = new Welcome();
        if (welcomeWindow.ShowDialog() == true)
        {
            _startDate = welcomeWindow.getStartDate();
            _endDate = welcomeWindow.getEndDate();
            _studentFilePath = DataHandler.GetDataFilePath(welcomeWindow.getSaveFileName());

            if (File.Exists(_studentFilePath))
            {
                _allStudents = StudentManager.LoadFromJson(File.ReadAllText(_studentFilePath));
            }
            else
            {
                _allStudents = new List<Student>();
            }

            // Update the window title with the IEP filename
            string iepFileName = System.IO.Path.GetFileName(_studentFilePath);
            this.Title = $"TherapyTime - {iepFileName}";

            PopulateDeleteStudentMenu();
            CreateButtonGrid();
        }
        else
        {
            Close();
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
        if (_allStudents.Count == 0)
        {
            MessageBox.Show("No students available to add a session.", "Info");
            return;
        }

        // Open AddSessionWindow with all students in dropdown
        AddSessionWindow addSessionWindow = new AddSessionWindow(_allStudents)
        {
            Owner = this
        };

        if (addSessionWindow.ShowDialog() == true)
        {
            var student = addSessionWindow.SelectedStudent!;
            var date = addSessionWindow.SelectedDate;
            int minutes = addSessionWindow.Minutes;

            if (!student.HasSessionOn(date))
            {
                student.ScheduleSession(date, minutes);
                SaveStudents();
                MessageBox.Show($"Session added for {student.Name} on {date:MM/dd/yyyy} ({minutes} minutes).", "Info");
            }
            else
            {
                MessageBox.Show($"{student.Name} already has a session on {date:MM/dd/yyyy}.", "Info");
            }
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

                // Check if the file already exists
                if (File.Exists(_studentFilePath))
                {
                    _allStudents = StudentManager.LoadFromJson(File.ReadAllText(_studentFilePath));
                    MessageBox.Show($"IEP loaded with {_allStudents.Count} students.", "Info");
                }
                else
                {
                    _allStudents = new List<Student>();
                    SaveStudents(); // save empty IEP
                    MessageBox.Show("New IEP created.", "Info");
                }

                // Update the window title with the IEP filename
                string iepFileName = System.IO.Path.GetFileName(_studentFilePath);
                this.Title = $"TherapyTime - {iepFileName}";

                PopulateDeleteStudentMenu();
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
            PopulateDeleteStudentMenu();
            MessageBox.Show($"Student '{studentName}' added.", "Info");
        }
        else
        {
            MessageBox.Show("No name entered. Student not added.", "Info");
        }
    }

    

    private void PopulateDeleteStudentMenu()
    {
        DeleteStudentMenu.Items.Clear();

        if (_allStudents.Count == 0)
        {
            MenuItem emptyItem = new MenuItem { Header = "(No students)" };
            emptyItem.IsEnabled = false;
            DeleteStudentMenu.Items.Add(emptyItem);
            return;
        }

        foreach (var student in _allStudents)
        {
            MenuItem studentItem = new MenuItem
            {
                Header = student.Name,
                Tag = student
            };
            studentItem.Click += DeleteStudent_Click;
            DeleteStudentMenu.Items.Add(studentItem);
        }
    }

    private void DeleteStudent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is Student student)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete {student.Name}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _allStudents.Remove(student);
                SaveStudents();
                PopulateDeleteStudentMenu();
                MessageBox.Show($"{student.Name} has been deleted.", "Info");
            }
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
        string json = StudentManager.SaveToJson(_allStudents);
        File.WriteAllText(_studentFilePath, json);
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
                EditDaySessionsWindow editWindow = new EditDaySessionsWindow(_allStudents, day)
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