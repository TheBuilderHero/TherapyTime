using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;           // For Window, RoutedEventArgs, MessageBox
using System.Windows.Controls;  // For ComboBox, Button, etc.

namespace TherapyTime;

public partial class AddSessionWindow : Window
{
    public Student? SelectedStudent { get; private set; }
    public DateTime SelectedDate { get; private set; }
    public int Minutes { get; private set; }
    public SessionCode SessionCode { get; private set; }
    public TimeSpan SessionTime { get; private set; } = new TimeSpan(8, 0, 0);

    private DateTime _iepStartDate;
    private DateTime _iepEndDate;
    private bool _dateLocked;
    private readonly List<Student> _allStudents;

    private class SessionCodeItem
    {
        public SessionCode Code { get; set; }
        public string DisplayText { get; set; } = string.Empty;

        public SessionCodeItem(SessionCode code, string description)
        {
            Code = code;
            DisplayText = $"{code} - {description}";
        }
    }

    private class SessionTimeItem
    {
        public TimeSpan Time { get; set; }
        public string DisplayText { get; set; } = string.Empty;

        public SessionTimeItem(TimeSpan time)
        {
            Time = time;
            DisplayText = DateTime.Today.Add(time).ToString("hh:mm tt");
        }
    }

    public AddSessionWindow(List<Student> students, DateTime iepStartDate, DateTime iepEndDate, DateTime? preselectedDate = null, bool lockDate = false)
    {
        InitializeComponent();

        _allStudents = students;
        _iepStartDate = iepStartDate;
        _iepEndDate = iepEndDate;
        _dateLocked = lockDate;

        // Students dropdown
        StudentComboBox.ItemsSource = students;
        StudentComboBox.DisplayMemberPath = "Name";
        if (students.Count > 0)
            StudentComboBox.SelectedIndex = 0;

        // Date
        SessionDatePicker.SelectedDate = preselectedDate ?? DateTime.Today;
        SessionDatePicker.IsEnabled = !_dateLocked;

        // Time dropdown (every 15 minutes)
        var timeItems = new List<SessionTimeItem>();
        for (int hour = 7; hour <= 18; hour++)
        {
            for (int minute = 0; minute < 60; minute += 15)
            {
                timeItems.Add(new SessionTimeItem(new TimeSpan(hour, minute, 0)));
            }
        }
        SessionTimeComboBox.ItemsSource = timeItems;
        SessionTimeComboBox.DisplayMemberPath = "DisplayText";
        SessionTimeComboBox.SelectedItem = timeItems.FirstOrDefault(t => t.Time == new TimeSpan(8, 0, 0)) ?? timeItems.First();

        // Minutes dropdown (increments of 5)
        for (int i = 5; i <= 75; i += 5)
            MinutesComboBox.Items.Add(i);
        MinutesComboBox.SelectedIndex = 5; // default 30 minutes

        // Session codes dropdown with descriptions
        var sessionCodeItems = new List<SessionCodeItem>
        {
            new SessionCodeItem(SessionCode.IC, "Incomplete - session has not yet taken place"),
            new SessionCodeItem(SessionCode.T, "Completed - session was successfully conducted"),
            new SessionCodeItem(SessionCode.NM, "Needs Makeup - session missed and requires makeup"),
            new SessionCodeItem(SessionCode.MU, "Makeup - replacement session for a missed one"),
            new SessionCodeItem(SessionCode.R, "Refused - session refused, no makeup needed"),
            new SessionCodeItem(SessionCode.A, "Absent - student absent, no makeup needed"),
            new SessionCodeItem(SessionCode.SU, "Unavailable - student unavailable, no makeup needed")
        };
        SessionCodeComboBox.ItemsSource = sessionCodeItems;
        SessionCodeComboBox.DisplayMemberPath = "DisplayText";
        SessionCodeComboBox.SelectedIndex = 0;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (StudentComboBox.SelectedItem is not Student student)
        {
            MessageBox.Show("Please select a student.", "Error");
            return;
        }

        if (SessionDatePicker.SelectedDate == null)
        {
            MessageBox.Show("Please select a valid date.", "Error");
            return;
        }

        if (MinutesComboBox.SelectedItem == null || !int.TryParse(MinutesComboBox.SelectedItem.ToString(), out int minutes))
        {
            MessageBox.Show("Please select valid minutes.", "Error");
            return;
        }

        if (SessionCodeComboBox.SelectedItem == null)
        {
            MessageBox.Show("Please select a session code.", "Error");
            return;
        }

        if (SessionTimeComboBox.SelectedItem is not SessionTimeItem selectedTime)
        {
            MessageBox.Show("Please select a valid time.", "Error");
            return;
        }

        SelectedStudent = student;
        SelectedDate = SessionDatePicker.SelectedDate.Value;
        Minutes = minutes;
        SessionCode = ((SessionCodeItem)SessionCodeComboBox.SelectedItem).Code;
        SessionTime = selectedTime.Time;

        // Validate that the selected date is within the IEP date range
        if (SelectedDate < _iepStartDate || SelectedDate > _iepEndDate)
        {
            MessageBox.Show($"The selected date ({SelectedDate:MM/dd/yyyy}) is outside the current IEP date range ({_iepStartDate:MM/dd/yyyy} to {_iepEndDate:MM/dd/yyyy}).\n\nPlease select a date within the IEP period.", "Date Outside IEP Range", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Check for overlapping sessions across all students on the selected date
        var newStart = selectedTime.Time;
        var newEnd = newStart.Add(TimeSpan.FromMinutes(minutes));
        var conflict = _allStudents
            .SelectMany(s => s.Sessions
                .Where(sess => sess.Date.Date == SelectedDate.Date)
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

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void HelpBubble_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Add Session Help\n\n" +
            "Purpose:\n" +
            "- Create a new therapy session for a selected student and date/time.\n\n" +
            "How to use this window:\n" +
            "1. Select Student, Date, Time, Minutes, and Session Code.\n" +
            "2. Click Add to create the session.\n\n" +
            "What validation means:\n" +
            "- Date must be inside the active IEP range.\n" +
            "- Session time cannot overlap any other session on the same date across all students.\n" +
            "- Session codes define whether the session is completed, missed, makeup, or excused.",
            "Add Session Help",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}