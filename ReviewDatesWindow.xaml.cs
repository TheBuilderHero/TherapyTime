using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TherapyTime;

public partial class ReviewDatesWindow : Window
{
    private readonly Action<Student>? _openStudentInfoAction;

    public ReviewDatesWindow(List<Student> students, Action<Student>? openStudentInfoAction = null)
    {
        InitializeComponent();
        _openStudentInfoAction = openStudentInfoAction;

        DateTime today = DateTime.Today;
        DateTime reminderEnd = today.AddDays(30);
        SummaryText.Text = $"Showing all students. Highlighted dates fall between {today:MM/dd/yyyy} and {reminderEnd:MM/dd/yyyy}.";

        ReviewDatesDataGrid.ItemsSource = students
            .OrderBy(student => student.Name)
            .Select(student => new ReviewDatesViewModel(student, today, reminderEnd))
            .ToList();
    }

    private void HelpBubble_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Review and Reevaluation Dates Help\n\n" +
            "Purpose:\n" +
            "- Show annual review and reevaluation compliance dates for all students in one place.\n\n" +
            "How to use this window:\n" +
            "- Review rows sorted by student name.\n" +
            "- Check Past Annual Reviews, Future Annual Reviews, and 3-Year Reevaluation columns.\n\n" +
            "What highlight means:\n" +
            "- Bold yellow text marks dates within the next 30 days from today.",
            "Review Dates Help",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void StudentName_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Student student)
            return;

        _openStudentInfoAction?.Invoke(student);
    }
}

public class ReviewDatesViewModel
{
    private static readonly Brush DefaultBrush = new SolidColorBrush(Color.FromRgb(55, 65, 81));
    private static readonly Brush HighlightBrush = new SolidColorBrush(Color.FromRgb(161, 98, 7));

    public ReviewDatesViewModel(Student student, DateTime today, DateTime reminderEnd)
    {
        StudentRef = student;
        StudentName = student.Name;
        StudentToolTip = student.StudentSummaryForTooltip;
        PastAnnualReviewsText = student.PastAnnualReviews.Count == 0
            ? "-"
            : string.Join(", ", student.PastAnnualReviews.OrderBy(date => date).Select(date => date.ToString("MM/dd/yyyy")));

        var futureAnnualReviews = student.FutureAnnualReviews.OrderBy(date => date).ToList();
        FutureAnnualReviewsText = futureAnnualReviews.Count == 0
            ? "-"
            : string.Join(", ", futureAnnualReviews.Select(date => date.ToString("MM/dd/yyyy")));

        bool futureAnnualWithin30Days = futureAnnualReviews.Any(date => date.Date >= today && date.Date <= reminderEnd);
        FutureAnnualReviewsBrush = futureAnnualWithin30Days ? HighlightBrush : DefaultBrush;
        FutureAnnualReviewsFontWeight = futureAnnualWithin30Days ? FontWeights.Bold : FontWeights.Normal;

        if (student.NextThreeYearReevaluation.HasValue)
        {
            var reevaluationDate = student.NextThreeYearReevaluation.Value.Date;
            ReevaluationText = reevaluationDate.ToString("MM/dd/yyyy");
            bool reevaluationWithin30Days = reevaluationDate >= today && reevaluationDate <= reminderEnd;
            ReevaluationBrush = reevaluationWithin30Days ? HighlightBrush : DefaultBrush;
            ReevaluationFontWeight = reevaluationWithin30Days ? FontWeights.Bold : FontWeights.Normal;
        }
        else
        {
            ReevaluationText = "-";
            ReevaluationBrush = DefaultBrush;
            ReevaluationFontWeight = FontWeights.Normal;
        }
    }

    public Student StudentRef { get; }
    public string StudentName { get; }
    public string StudentToolTip { get; }
    public string PastAnnualReviewsText { get; }
    public string FutureAnnualReviewsText { get; }
    public Brush FutureAnnualReviewsBrush { get; }
    public FontWeight FutureAnnualReviewsFontWeight { get; }
    public string ReevaluationText { get; }
    public Brush ReevaluationBrush { get; }
    public FontWeight ReevaluationFontWeight { get; }
}