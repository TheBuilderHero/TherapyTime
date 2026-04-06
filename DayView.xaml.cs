using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TherapyTime;

/// <summary>
/// Interaction logic for DayView.xaml
/// </summary>
public partial class DayView : Window
{
    private List<Student> _students;
    private DateTime _selectedDate;

    public DayView(List<Student> students, DateTime selectedDate)
    {
        InitializeComponent();

        _students = students;
        _selectedDate = selectedDate;

        RefreshDataGrid();
    }

    private class SessionRow
    {
        public Student StudentRef { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int CurrentMonthMinutes { get; set; }
        public DateTime? NextThreeYearReevaluation { get; set; } // nullable to avoid CS0266
    }

    private void RefreshDataGrid()
    {
        var sessionsOnDate = _students
            .Where(s => s.HasSessionOn(_selectedDate))
            .Select(s => new SessionRow
            {
                StudentRef = s,
                Name = s.Name,
                CurrentMonthMinutes = s.TotalMinutesForMonth(_selectedDate.Year, _selectedDate.Month),
                NextThreeYearReevaluation = s.NextThreeYearReevaluation // nullable handled
            })
            .ToList();

        userDataGrid.ItemsSource = sessionsOnDate;
    }

    private void MeetingComplete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SessionRow row)
        {
            Student student = row.StudentRef;

            // Find the session on this date
            var session = student.Sessions.FirstOrDefault(s => s.Date.Date == _selectedDate.Date);

            if (session != null && session.Code != SessionCode.T)
            {
                // Mark session as complete
                session.Code = SessionCode.T;

                // Refresh the DataGrid
                RefreshDataGrid();
            }
            else
            {
                MessageBox.Show($"{student.Name} already has this session completed.", "Info");
            }
        }
    }

    private void HelpBubble_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Day View Help\n\n" +
            "Purpose:\n" +
            "- Legacy day-level summary of students with sessions on the selected date.\n\n" +
            "How to use this window:\n" +
            "- Review minutes and reevaluation info in the grid.\n" +
            "- Use Meeting Complete to mark a listed session as complete.\n\n" +
            "Note:\n" +
            "- Main day editing and validation workflows are handled in the Edit Day Sessions window.",
            "Day View Help",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}