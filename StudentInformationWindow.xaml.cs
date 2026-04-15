using System.Windows;
using System.Windows.Controls;

namespace TherapyTime;

public partial class StudentInformationWindow : Window
{
    private readonly List<Student> _students;
    private readonly Action _persistChanges;
    private Student? _selectedStudent;

    public StudentInformationWindow(List<Student> students, Action persistChanges, Student? preselectedStudent = null)
    {
        InitializeComponent();

        _students = students.OrderBy(s => s.Name).ToList();
        _persistChanges = persistChanges;

        StudentListBox.ItemsSource = _students;

        if (preselectedStudent != null)
        {
            StudentListBox.SelectedItem = _students.FirstOrDefault(s => s.Id == preselectedStudent.Id) ?? _students.FirstOrDefault();
        }
        else
        {
            StudentListBox.SelectedItem = _students.FirstOrDefault();
        }
    }

    private void StudentListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedStudent = StudentListBox.SelectedItem as Student;
        LoadSelectedStudent();
    }

    private void LoadSelectedStudent()
    {
        if (_selectedStudent == null)
            return;

        FirstNameTextBox.Text = _selectedStudent.FirstName;
        LastNameTextBox.Text = _selectedStudent.LastName;
        RequiredMinutesTextBox.Text = _selectedStudent.MonthlyRequiredMinutes.ToString();
        ArchivedCheckBox.IsChecked = _selectedStudent.IsArchived;

        MainContactNameTextBox.Text = _selectedStudent.MainContactName;
        MainContactPhoneTextBox.Text = _selectedStudent.MainContactPhone;
        MainContactEmailTextBox.Text = _selectedStudent.MainContactEmail;
        PreferPhoneCheckBox.IsChecked = _selectedStudent.PreferPhoneContact;
        PreferEmailCheckBox.IsChecked = _selectedStudent.PreferEmailContact;

        FutureAnnualReviewPicker.SelectedDate = _selectedStudent.FutureAnnualReviews.OrderByDescending(d => d).FirstOrDefault();
        NextThreeYearReevaluationPicker.SelectedDate = _selectedStudent.NextThreeYearReevaluation;

        BuildSessionHistoryTree(_selectedStudent);
    }

    private void BuildSessionHistoryTree(Student student)
    {
        SessionHistoryTree.Items.Clear();

        var groupedByYear = student.Sessions
            .OrderByDescending(s => s.Date)
            .GroupBy(s => s.Date.Year)
            .OrderByDescending(g => g.Key);

        foreach (var yearGroup in groupedByYear)
        {
            var yearItem = new TreeViewItem { Header = yearGroup.Key.ToString(), IsExpanded = false };

            var months = yearGroup
                .GroupBy(s => s.Date.Month)
                .OrderBy(g => g.Key);

            foreach (var monthGroup in months)
            {
                string monthName = new DateTime(yearGroup.Key, monthGroup.Key, 1).ToString("MMMM");
                var monthItem = new TreeViewItem
                {
                    Header = $"{monthName} ({monthGroup.Count()} sessions)",
                    IsExpanded = false
                };

                foreach (var session in monthGroup.OrderBy(s => s.SessionDateTime))
                {
                    monthItem.Items.Add(new TreeViewItem
                    {
                        Header = $"{session.Date:MM/dd/yyyy} {DateTime.Today.Add(session.TimeOfDay):hh:mm tt} - {session.Minutes} min ({session.Code})",
                        IsExpanded = false
                    });
                }

                yearItem.Items.Add(monthItem);
            }

            SessionHistoryTree.Items.Add(yearItem);
        }
    }

    private bool SaveCurrentStudent()
    {
        if (_selectedStudent == null)
            return false;

        string firstName = FirstNameTextBox.Text.Trim();
        string lastName = LastNameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            MessageBox.Show("Student first and last names are required.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        string candidateFullName = $"{firstName} {lastName}".Trim();
        bool duplicateName = _students.Any(s => s.Id != _selectedStudent.Id && string.Equals(s.Name.Trim(), candidateFullName, StringComparison.OrdinalIgnoreCase));
        if (duplicateName)
        {
            MessageBox.Show("Another student already has that name. Please use a more descriptive name.", "Duplicate Student Name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!int.TryParse(RequiredMinutesTextBox.Text.Trim(), out int requiredMinutes) || requiredMinutes <= 0)
        {
            MessageBox.Show("Required minutes must be a whole number greater than 0.", "Invalid Required Minutes", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        _selectedStudent.FirstName = firstName;
        _selectedStudent.LastName = lastName;
        _selectedStudent.MonthlyRequiredMinutes = requiredMinutes;
        _selectedStudent.TotalMinutesRequired = requiredMinutes;
        _selectedStudent.IsArchived = ArchivedCheckBox.IsChecked == true;

        _selectedStudent.MainContactName = MainContactNameTextBox.Text.Trim();
        _selectedStudent.MainContactPhone = MainContactPhoneTextBox.Text.Trim();
        _selectedStudent.MainContactEmail = MainContactEmailTextBox.Text.Trim();
        _selectedStudent.PreferPhoneContact = PreferPhoneCheckBox.IsChecked == true;
        _selectedStudent.PreferEmailContact = PreferEmailCheckBox.IsChecked == true;

        DateTime? futureAnnual = FutureAnnualReviewPicker.SelectedDate?.Date;
        _selectedStudent.FutureAnnualReviews = futureAnnual.HasValue ? new List<DateTime> { futureAnnual.Value } : new List<DateTime>();
        _selectedStudent.NextThreeYearReevaluation = NextThreeYearReevaluationPicker.SelectedDate?.Date;

        _persistChanges();
        StudentListBox.Items.Refresh();
        BuildSessionHistoryTree(_selectedStudent);
        return true;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (SaveCurrentStudent())
        {
            MessageBox.Show("Student information saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void SaveAndExit_Click(object sender, RoutedEventArgs e)
    {
        if (SaveCurrentStudent())
        {
            DialogResult = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
