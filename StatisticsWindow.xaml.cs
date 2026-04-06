using System.Windows;
using System.Windows.Media;

namespace TherapyTime;

public partial class StatisticsWindow : Window
{
    public StatisticsWindow(List<Student> students, DateTime iepStartDate, DateTime iepEndDate)
    {
        InitializeComponent();

        RangeText.Text = $"{iepStartDate:MM/dd/yyyy} - {iepEndDate:MM/dd/yyyy}";

        var viewModels = students
            .OrderBy(s => s.Name)
            .Select(s => BuildStudentStatistics(s, iepStartDate, iepEndDate))
            .ToList();

        StudentStatsItemsControl.ItemsSource = viewModels;
    }

    private void HelpBubble_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Student Minutes and Sessions Help\n\n" +
            "Purpose:\n" +
            "- Show per-student minute totals and session details for the current IEP.\n\n" +
            "How to use this window:\n" +
            "- Expand a student card to view individual sessions.\n" +
            "- Review Completed, Scheduled, Excused, Remaining, and Therapy Balance values.\n\n" +
            "What colors mean:\n" +
            "- Green: at or above required threshold.\n" +
            "- Amber: scheduled to meet target but not fully completed yet.\n" +
            "- Red: below required trajectory.",
            "Statistics Help",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static StudentStatisticsViewModel BuildStudentStatistics(Student student, DateTime iepStartDate, DateTime iepEndDate)
    {
        var sessions = student.Sessions
            .Where(sess =>
                // Primary filter: current IEP range.
                (sess.Date.Date >= iepStartDate.Date && sess.Date.Date <= iepEndDate.Date)
                // Safety include: always surface same-day sessions.
                || sess.Date.Date == DateTime.Today)
            .GroupBy(sess => sess.Id)
            .Select(g => g.First())
            .OrderBy(sess => sess.SessionDateTime)
            .Select(sess => new SessionStatisticsViewModel(sess))
            .ToList();

        int requiredTime = student.TotalMinutesRequired;
        int completedMinutes = sessions
            .Where(s => s.IsCompleted && (s.Code == SessionCode.T || s.Code == SessionCode.MU))
            .Sum(s => s.Minutes);
        int excusedMinutes = sessions
            .Where(s => s.IsCompleted && (s.Code == SessionCode.R || s.Code == SessionCode.A || s.Code == SessionCode.SU))
            .Sum(s => s.Minutes);
        int scheduledMinutes = sessions
            .Where(s => s.Code != SessionCode.NM)
            .Sum(s => s.Minutes);
        int effectiveCompletedMinutes = completedMinutes + excusedMinutes;
        int balanceMinutes = effectiveCompletedMinutes - requiredTime;
        int remainingMinutes = Math.Max(requiredTime - completedMinutes - excusedMinutes, 0);

        string statusText;
        Brush statusBrush;
        Brush balanceBrush;

        if (completedMinutes >= requiredTime)
        {
            statusText = "Completed to required amount";
            statusBrush = new SolidColorBrush(Color.FromRgb(167, 243, 208));
            balanceBrush = new SolidColorBrush(Color.FromRgb(22, 101, 52));
        }
        else if (scheduledMinutes >= requiredTime)
        {
            statusText = "Scheduled to required amount";
            statusBrush = new SolidColorBrush(Color.FromRgb(253, 230, 138));
            balanceBrush = new SolidColorBrush(Color.FromRgb(161, 98, 7));
        }
        else
        {
            statusText = "Below required amount";
            statusBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202));
            balanceBrush = new SolidColorBrush(Color.FromRgb(153, 27, 27));
        }

        return new StudentStatisticsViewModel
        {
            StudentName = student.Name,
            NeededMinutes = student.MonthlyRequiredMinutes,
            RequiredTime = requiredTime,
            CompletedMinutes = completedMinutes,
            ScheduledMinutes = scheduledMinutes,
            ExcusedMinutes = excusedMinutes,
            RemainingMinutes = remainingMinutes,
            TherapyBalance = balanceMinutes,
            TherapyBalanceBrush = balanceBrush,
            ScheduledMinutesBrush = completedMinutes >= requiredTime
                ? new SolidColorBrush(Color.FromRgb(22, 101, 52))
                : scheduledMinutes >= requiredTime
                    ? new SolidColorBrush(Color.FromRgb(161, 98, 7))
                    : new SolidColorBrush(Color.FromRgb(153, 27, 27)),
            StatusText = statusText,
            StatusBrush = statusBrush,
            Sessions = sessions
        };
    }
}

public class StudentStatisticsViewModel
{
    public string StudentName { get; set; } = string.Empty;
    public int NeededMinutes { get; set; }
    public int RequiredTime { get; set; }
    public int CompletedMinutes { get; set; }
    public int ScheduledMinutes { get; set; }
    public int ExcusedMinutes { get; set; }
    public int RemainingMinutes { get; set; }
    public int TherapyBalance { get; set; }
    public Brush TherapyBalanceBrush { get; set; } = new SolidColorBrush(Colors.Black);
    public Brush ScheduledMinutesBrush { get; set; } = new SolidColorBrush(Colors.Black);
    public string StatusText { get; set; } = string.Empty;
    public Brush StatusBrush { get; set; } = new SolidColorBrush(Colors.LightGray);
    public List<SessionStatisticsViewModel> Sessions { get; set; } = new List<SessionStatisticsViewModel>();

    public string NeededMinutesText => $"Needed: {NeededMinutes} min";
    public string RequiredTimeText => $"Required: {RequiredTime} min";
    public string CompletedMinutesText => $"Completed: {CompletedMinutes} min";
    public string ScheduledMinutesText => $"Scheduled: {ScheduledMinutes} min";
    public string ExcusedMinutesText => $"Excused (R/A/SU): {ExcusedMinutes} min";
    public string RemainingMinutesText => $"Remaining: {RemainingMinutes} min";
    public string TherapyBalanceText => $"Therapy Balance: {(TherapyBalance >= 0 ? "+" : "")}{TherapyBalance} min";
}

public class SessionStatisticsViewModel
{
    public SessionStatisticsViewModel(Session session)
    {
        Date = session.Date.Date;
        TimeOfDay = session.TimeOfDay;
        Minutes = session.Minutes;
        Code = session.Code;
        IsCompleted = session.IsCompleted;

        if (IsCompleted)
        {
            RowBackground = new SolidColorBrush(Color.FromRgb(220, 252, 231));
            CompletionStatusText = "Completed";
        }
        else if (Date.Date >= DateTime.Today)
        {
            RowBackground = new SolidColorBrush(Color.FromRgb(254, 249, 195));
            CompletionStatusText = "Upcoming not completed";
        }
        else
        {
            RowBackground = new SolidColorBrush(Color.FromRgb(254, 226, 226));
            CompletionStatusText = "Past not completed";
        }
    }

    public DateTime Date { get; }
    public TimeSpan TimeOfDay { get; }
    public int Minutes { get; }
    public SessionCode Code { get; }
    public bool IsCompleted { get; }
    public Brush RowBackground { get; }
    public string CompletionStatusText { get; }

    public string DateText => Date.ToString("MM/dd/yyyy");
    public string TimeText => DateTime.Today.Add(TimeOfDay).ToString("hh:mm tt");
    public string CodeText => Code.ToString();
}
