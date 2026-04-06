using System.Windows;

namespace TherapyTime;

public partial class AddStudentWindow : Window
{
    public string StudentName { get; private set; } = string.Empty;
    public int MonthlyRequiredMinutes { get; private set; }
    public DateTime FutureAnnualReview { get; private set; }
    public DateTime NextThreeYearReevaluation { get; private set; }
    public DateTime? PastAnnualReview { get; private set; }

    public AddStudentWindow()
    {
        InitializeComponent();

        FutureAnnualReviewPicker.SelectedDate = DateTime.Today;
        NextThreeYearReevaluationPicker.SelectedDate = DateTime.Today;
        PastAnnualReviewPicker.SelectedDate = DateTime.Today;
        StudentNameTextBox.Focus();
    }

    private void AddPastAnnualReviewCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        PastAnnualReviewPicker.IsEnabled = AddPastAnnualReviewCheckBox.IsChecked == true;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        string studentName = StudentNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(studentName))
        {
            MessageBox.Show("Student name is required.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(RequiredMinutesTextBox.Text.Trim(), out int requiredMinutes) || requiredMinutes <= 0)
        {
            MessageBox.Show("Required minutes must be a whole number greater than 0.", "Invalid Required Minutes", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (FutureAnnualReviewPicker.SelectedDate == null)
        {
            MessageBox.Show("Future Annual Review is required.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (NextThreeYearReevaluationPicker.SelectedDate == null)
        {
            MessageBox.Show("Next 3-Year Reevaluation is required.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (AddPastAnnualReviewCheckBox.IsChecked == true && PastAnnualReviewPicker.SelectedDate == null)
        {
            MessageBox.Show("Select a Past Annual Review date or uncheck the option.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        StudentName = studentName;
        MonthlyRequiredMinutes = requiredMinutes;
        FutureAnnualReview = FutureAnnualReviewPicker.SelectedDate.Value.Date;
        NextThreeYearReevaluation = NextThreeYearReevaluationPicker.SelectedDate.Value.Date;
        PastAnnualReview = AddPastAnnualReviewCheckBox.IsChecked == true
            ? PastAnnualReviewPicker.SelectedDate?.Date
            : null;

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void HelpBubble_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Add Student Help\n\n" +
            "Purpose:\n" +
            "- Add a student with required therapy targets and compliance dates.\n\n" +
            "Required fields:\n" +
            "- Student Name\n" +
            "- Required Minutes\n" +
            "- Future Annual Review\n" +
            "- Next 3-Year Reevaluation\n\n" +
            "Optional field:\n" +
            "- Past Annual Review\n\n" +
            "What it means:\n" +
            "- These dates are used for reminders and reporting in Statistics.\n" +
            "- Required Minutes becomes the student's IEP minute target.",
            "Add Student Help",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}