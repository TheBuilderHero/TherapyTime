using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TherapyTime;

public partial class StatisticsWindow : Window
{
    private readonly List<Student> _students;
    private readonly IepYearFile _iepYear;
    private readonly bool _showYearStatistics;
    private readonly Action<Student>? _openStudentInfoAction;

    public StatisticsWindow(List<Student> students, IepYearFile iepYear, bool showYearStatistics, Action<Student>? openStudentInfoAction = null)
    {
        InitializeComponent();

        _students = students.OrderBy(s => s.Name).ToList();
        _iepYear = iepYear;
        _showYearStatistics = showYearStatistics;
        _openStudentInfoAction = openStudentInfoAction;

        if (_showYearStatistics)
        {
            MonthSelectorPanel.Visibility = Visibility.Collapsed;
            ModeText.Text = "Current IEP year statistics";
            RangeText.Text = $"{_iepYear.SchoolYearStartDate:MM/dd/yyyy} - {_iepYear.SchoolYearEndDate:MM/dd/yyyy}";
            RefreshYearStatistics();
        }
        else
        {
            ModeText.Text = "Selected IEP month statistics";
            var months = _iepYear.Months.OrderBy(m => m.StartDate).ToList();
            MonthComboBox.ItemsSource = months;

            var todayMonth = months.FirstOrDefault(m => DateTime.Today.Date >= m.StartDate.Date && DateTime.Today.Date <= m.EndDate.Date);
            MonthComboBox.SelectedItem = todayMonth ?? months.FirstOrDefault();
        }
    }

    private void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MonthComboBox.SelectedItem is not IepMonthDefinition month)
            return;

        RangeText.Text = $"{month.Name}: {month.StartDate:MM/dd/yyyy} - {month.EndDate:MM/dd/yyyy}";
        RefreshMonthlyStatistics(month.StartDate, month.EndDate);
    }

    private void RefreshMonthlyStatistics(DateTime monthStart, DateTime monthEnd)
    {
        var viewModels = _students
            .Select(s => BuildMonthlyStudentStatistics(s, monthStart, monthEnd))
            .OrderBy(vm => vm.StudentName)
            .ToList();

        StudentStatsItemsControl.ItemsSource = viewModels;
    }

    private void RefreshYearStatistics()
    {
        DateTime effectiveEnd = DateTime.Today.Date < _iepYear.SchoolYearStartDate.Date
            ? _iepYear.SchoolYearStartDate.Date
            : DateTime.Today.Date > _iepYear.SchoolYearEndDate.Date
                ? _iepYear.SchoolYearEndDate.Date
                : DateTime.Today.Date;

        var viewModels = _students
            .Select(s => BuildYearStudentStatistics(s, _iepYear.SchoolYearStartDate, effectiveEnd, _iepYear.SchoolYearEndDate))
            .OrderBy(vm => vm.StudentName)
            .ToList();

        StudentStatsItemsControl.ItemsSource = viewModels;
    }

    private static StudentStatisticsViewModel BuildMonthlyStudentStatistics(Student student, DateTime monthStart, DateTime monthEnd)
    {
        var sessions = student.Sessions
            .Where(sess => sess.Date.Date >= monthStart.Date && sess.Date.Date <= monthEnd.Date)
            .GroupBy(sess => sess.Id)
            .Select(g => g.First())
            .OrderBy(sess => sess.SessionDateTime)
            .Select(sess => new SessionStatisticsViewModel(sess))
            .ToList();

        int requiredTime = student.MonthlyRequiredMinutes;
        return BuildStatisticsViewModel(student, sessions, requiredTime, "Monthly");
    }

    private static StudentStatisticsViewModel BuildYearStudentStatistics(Student student, DateTime yearStart, DateTime effectiveEnd, DateTime yearEnd)
    {
        var sessions = student.Sessions
            .Where(sess => sess.Date.Date >= yearStart.Date && sess.Date.Date <= effectiveEnd.Date)
            .GroupBy(sess => sess.Id)
            .Select(g => g.First())
            .OrderBy(sess => sess.SessionDateTime)
            .Select(sess => new SessionStatisticsViewModel(sess))
            .ToList();

        double requiredToDate = CalculateRequiredMinutesToDate(student, yearStart, effectiveEnd, yearEnd);
        int requiredTime = (int)Math.Round(requiredToDate, MidpointRounding.AwayFromZero);

        return BuildStatisticsViewModel(student, sessions, requiredTime, "Year-to-date");
    }

    private static double CalculateRequiredMinutesToDate(Student student, DateTime yearStart, DateTime effectiveEnd, DateTime yearEnd)
    {
        if (effectiveEnd.Date <= yearStart.Date)
            return 0;

        int totalDays = (yearEnd.Date - yearStart.Date).Days + 1;
        int elapsedDays = (effectiveEnd.Date - yearStart.Date).Days + 1;

        double yearProgress = totalDays > 0 ? (double)elapsedDays / totalDays : 0;
        yearProgress = Math.Max(0, Math.Min(1, yearProgress));

        return student.MonthlyRequiredMinutes * 12 * yearProgress;
    }

    private static StudentStatisticsViewModel BuildStatisticsViewModel(Student student, List<SessionStatisticsViewModel> sessions, int requiredTime, string scopeLabel)
    {
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
        int remainingMinutes = Math.Max(requiredTime - effectiveCompletedMinutes, 0);

        string statusText;
        Brush statusBrush;
        Brush balanceBrush;

        if (effectiveCompletedMinutes >= requiredTime)
        {
            statusText = $"{scopeLabel}: On Track";
            statusBrush = new SolidColorBrush(Color.FromRgb(167, 243, 208));
            balanceBrush = new SolidColorBrush(Color.FromRgb(22, 101, 52));
        }
        else if (scheduledMinutes >= requiredTime)
        {
            statusText = $"{scopeLabel}: Scheduled To Target";
            statusBrush = new SolidColorBrush(Color.FromRgb(253, 230, 138));
            balanceBrush = new SolidColorBrush(Color.FromRgb(161, 98, 7));
        }
        else
        {
            statusText = $"{scopeLabel}: Behind";
            statusBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202));
            balanceBrush = new SolidColorBrush(Color.FromRgb(153, 27, 27));
        }

        return new StudentStatisticsViewModel
        {
            StudentRef = student,
            StudentName = student.Name,
            StudentToolTip = student.StudentSummaryForTooltip,
            NeededMinutes = student.MonthlyRequiredMinutes,
            RequiredTime = requiredTime,
            CompletedMinutes = completedMinutes,
            ScheduledMinutes = scheduledMinutes,
            ExcusedMinutes = excusedMinutes,
            RemainingMinutes = remainingMinutes,
            TherapyBalance = balanceMinutes,
            TherapyBalanceBrush = balanceBrush,
            ScheduledMinutesBrush = effectiveCompletedMinutes >= requiredTime
                ? new SolidColorBrush(Color.FromRgb(22, 101, 52))
                : scheduledMinutes >= requiredTime
                    ? new SolidColorBrush(Color.FromRgb(161, 98, 7))
                    : new SolidColorBrush(Color.FromRgb(153, 27, 27)),
            StatusText = statusText,
            StatusBrush = statusBrush,
            Sessions = sessions
        };
    }

    private void StudentName_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Student student)
            return;

        _openStudentInfoAction?.Invoke(student);
    }

    private void HelpBubble_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Student Minutes and Sessions Help\n\n" +
            "Purpose:\n" +
            "- Show per-student minute totals and session details for either a selected IEP month or year-to-date.\n\n" +
            "How to use this window:\n" +
            "- Monthly mode: pick an IEP month from the dropdown.\n" +
            "- Year mode: values are calculated to today's date in the active IEP year.\n" +
            "- Click a student name to open their full information page.\n\n" +
            "What colors mean:\n" +
            "- Green: on track to target.\n" +
            "- Amber: scheduled to target but not completed yet.\n" +
            "- Red: behind target.",
            "Statistics Help",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}

public class StudentStatisticsViewModel
{
    public Student StudentRef { get; set; } = null!;
    public string StudentName { get; set; } = string.Empty;
    public string StudentToolTip { get; set; } = string.Empty;
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

    public string NeededMinutesText => $"Needed (Per Month): {NeededMinutes} min";
    public string RequiredTimeText => $"Required To Date: {RequiredTime} min";
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
