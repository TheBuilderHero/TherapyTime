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

    private DateTime _iepStartDate;
    private DateTime _iepEndDate;
    private bool _dateLocked;

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

    public AddSessionWindow(List<Student> students, DateTime iepStartDate, DateTime iepEndDate, DateTime? preselectedDate = null, bool lockDate = false)
    {
        InitializeComponent();

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

        // Minutes dropdown (increments of 10)
        for (int i = 10; i <= 120; i += 10)
            MinutesComboBox.Items.Add(i);
        MinutesComboBox.SelectedIndex = 2; // default 30 minutes

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

        SelectedStudent = student;
        SelectedDate = SessionDatePicker.SelectedDate.Value;
        Minutes = minutes;
        SessionCode = ((SessionCodeItem)SessionCodeComboBox.SelectedItem).Code;

        // Validate that the selected date is within the IEP date range
        if (SelectedDate < _iepStartDate || SelectedDate > _iepEndDate)
        {
            MessageBox.Show($"The selected date ({SelectedDate:MM/dd/yyyy}) is outside the current IEP date range ({_iepStartDate:MM/dd/yyyy} to {_iepEndDate:MM/dd/yyyy}).\n\nPlease select a date within the IEP period.", "Date Outside IEP Range", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}