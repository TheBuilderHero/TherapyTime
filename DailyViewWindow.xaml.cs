using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TherapyTime;

public partial class DailyViewWindow : Window
{
    private readonly List<Student> _students;
    private readonly DateTime _iepStart;
    private readonly DateTime _iepEnd;
    private DateTime _currentWeekStart; // always a Sunday

    public DailyViewWindow(List<Student> students, DateTime iepStart, DateTime iepEnd)
    {
        InitializeComponent();

        _students = students.OrderBy(s => s.Name).ToList();
        _iepStart = iepStart;
        _iepEnd = iepEnd;

        RangeText.Text = $"{iepStart:MM/dd/yyyy} – {iepEnd:MM/dd/yyyy}";

        // Anchor to the week containing today if today is in the IEP range, otherwise use IEP start
        DateTime anchor = (DateTime.Today >= iepStart.Date && DateTime.Today <= iepEnd.Date)
            ? DateTime.Today
            : iepStart;

        // Roll back to Sunday
        _currentWeekStart = anchor.AddDays(-(int)anchor.DayOfWeek);

        BuildGrid();
    }

    private void PrevWeek_Click(object sender, RoutedEventArgs e)
    {
        _currentWeekStart = _currentWeekStart.AddDays(-7);
        BuildGrid();
    }

    private void NextWeek_Click(object sender, RoutedEventArgs e)
    {
        _currentWeekStart = _currentWeekStart.AddDays(7);
        BuildGrid();
    }

    private void BuildGrid()
    {
        var weekDates = Enumerable.Range(0, 7)
            .Select(i => _currentWeekStart.AddDays(i))
            .ToList();

        WeekRangeText.Text = $"Week of {weekDates[0]:MMM d} – {weekDates[6]:MMM d, yyyy}";

        // Enable / disable navigation buttons
        PrevWeekButton.IsEnabled = _currentWeekStart.AddDays(-1).Date >= _iepStart.Date;
        NextWeekButton.IsEnabled = _currentWeekStart.AddDays(7).Date <= _iepEnd.Date;

        DailyGrid.Children.Clear();
        DailyGrid.RowDefinitions.Clear();
        DailyGrid.ColumnDefinitions.Clear();

        // Column 0 = student name, columns 1–7 = days of the week
        DailyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        for (int i = 0; i < 7; i++)
            DailyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

        // Row 0 = header, rows 1+ = one per student
        DailyGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        foreach (var _ in _students)
            DailyGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // --- Header row ---
        AddHeaderCell("Student", 0, 0, dimmed: false);
        for (int d = 0; d < 7; d++)
        {
            var date = weekDates[d];
            bool inIep = date.Date >= _iepStart.Date && date.Date <= _iepEnd.Date;
            // Show day-of-week abbreviation and date
            AddHeaderCell(date.ToString("ddd\nMMM d"), 0, d + 1, dimmed: !inIep);
        }

        // --- Student rows ---
        for (int r = 0; r < _students.Count; r++)
        {
            var student = _students[r];

            // Student name cell
            var nameBorder = MakeBorder(
                new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                new SolidColorBrush(Color.FromRgb(229, 231, 235)));
            nameBorder.Child = new TextBlock
            {
                Text = student.Name,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(nameBorder, r + 1);
            Grid.SetColumn(nameBorder, 0);
            DailyGrid.Children.Add(nameBorder);

            // Day cells
            for (int d = 0; d < 7; d++)
            {
                var date = weekDates[d];
                bool inIep = date.Date >= _iepStart.Date && date.Date <= _iepEnd.Date;

                var sessions = student.Sessions
                    .Where(s => s.Date.Date == date.Date)
                    .OrderBy(s => s.TimeOfDay)
                    .ToList();

                Brush cellBg = new SolidColorBrush(inIep
                    ? Color.FromRgb(255, 255, 255)
                    : Color.FromRgb(248, 249, 250));

                var cellBorder = MakeBorder(cellBg, new SolidColorBrush(Color.FromRgb(229, 231, 235)));

                if (sessions.Count == 0)
                {
                    cellBorder.Child = new TextBlock
                    {
                        Text = "–",
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    };
                }
                else
                {
                    bool isFuture = date.Date > DateTime.Today;
                    bool allCompleted = sessions.All(s => s.IsCompleted);

                    // Display one code per session, stacked vertically
                    string codeText = string.Join("\n", sessions.Select(s => s.Code.ToString()));

                    Brush bg;
                    Brush fg;

                    if (isFuture)
                    {
                        // Future date: neutral gray, no completion status
                        bg = new SolidColorBrush(Color.FromRgb(243, 244, 246));
                        fg = new SolidColorBrush(Color.FromRgb(107, 114, 128));
                    }
                    else if (allCompleted)
                    {
                        // All sessions completed: green
                        bg = new SolidColorBrush(Color.FromRgb(220, 252, 231));
                        fg = new SolidColorBrush(Color.FromRgb(22, 101, 52));
                    }
                    else
                    {
                        // Today or past with at least one incomplete: red
                        bg = new SolidColorBrush(Color.FromRgb(254, 226, 226));
                        fg = new SolidColorBrush(Color.FromRgb(153, 27, 27));
                    }

                    cellBorder.Background = bg;
                    cellBorder.Child = new TextBlock
                    {
                        Text = codeText,
                        FontWeight = FontWeights.Bold,
                        FontSize = 13,
                        Foreground = fg,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }

                Grid.SetRow(cellBorder, r + 1);
                Grid.SetColumn(cellBorder, d + 1);
                DailyGrid.Children.Add(cellBorder);
            }
        }
    }

    private void AddHeaderCell(string text, int row, int col, bool dimmed)
    {
        var border = MakeBorder(
            new SolidColorBrush(Color.FromRgb(243, 244, 246)),
            new SolidColorBrush(Color.FromRgb(209, 213, 219)));

        border.Child = new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            Foreground = new SolidColorBrush(dimmed
                ? Color.FromRgb(156, 163, 175)
                : Color.FromRgb(55, 65, 81)),
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        DailyGrid.Children.Add(border);
    }

    private static Border MakeBorder(Brush bg, Brush borderBrush) => new Border
    {
        Background = bg,
        BorderBrush = borderBrush,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(6, 8, 6, 8),
        Margin = new Thickness(1)
    };
}
