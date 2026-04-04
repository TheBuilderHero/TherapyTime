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

    public AddSessionWindow(List<Student> students, DateTime? preselectedDate = null)
    {
        InitializeComponent();

        // Students dropdown
        StudentComboBox.ItemsSource = students;
        StudentComboBox.DisplayMemberPath = "Name";
        if (students.Count > 0)
            StudentComboBox.SelectedIndex = 0;

        // Date
        SessionDatePicker.SelectedDate = preselectedDate ?? DateTime.Today;

        // Minutes dropdown (increments of 10)
        for (int i = 10; i <= 120; i += 10)
            MinutesComboBox.Items.Add(i);
        MinutesComboBox.SelectedIndex = 2; // default 30 minutes

        // Session codes dropdown from enum
        SessionCodeComboBox.ItemsSource = Enum.GetValues(typeof(SessionCode)).Cast<SessionCode>();
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
        SessionCode = (SessionCode)SessionCodeComboBox.SelectedItem;

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}