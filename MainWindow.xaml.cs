using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TherapyTime;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Button[,] _buttonGrid = null!;
    private int size = 10; // e.g., 10x10 grid\
    private int dayMax = 30; // total number of days that the IEP has which is 30.
    private DateTime _startDate;
    public MainWindow()
    {
        InitializeComponent();
        Welcome welcomeWindow = new Welcome();
        if(welcomeWindow.ShowDialog() == true)
        {
            _startDate = welcomeWindow.getStartDate();
            MessageBox.Show($"You picked: {_startDate}");
            CreateButtonGrid();
        } 
        else
        {
            //User closes the window
            Close(); //close the whole program.
        }
    }

    private void CreateButtonGrid()
    {
        panelGrid.Children.Clear();

        int totalDays = dayMax;       // 30
        DateTime startDate = _startDate;

        // Calculate what day of the week the start date is (0=Sunday)
        int startDayOfWeek = (int)startDate.DayOfWeek;

        int rows = (int)Math.Ceiling((startDayOfWeek + totalDays) / 7.0);
        panelGrid.Rows = rows;
        panelGrid.Columns = 7;

        int dayCounter = 1;

        // Loop through each cell in the grid
        for (int cell = 0; cell < rows * 7; cell++)
        {
            Button dayButton;

            if (cell < startDayOfWeek || dayCounter > totalDays)
            {
                // Empty space before the start day or after the last day
                dayButton = new Button
                {
                    IsEnabled = false,
                    Background = new SolidColorBrush(System.Windows.Media.Colors.Transparent),
                    BorderThickness = new Thickness(0)
                };
            }
            else
            {
                DateTime currentDay = startDate.AddDays(dayCounter - 1);

                dayButton = new Button
                {
                    Content = currentDay.Day.ToString(),
                    Tag = currentDay,
                    Width = 70,
                    Height = 70,
                    Margin = new Thickness(3),
                    Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(1),
                    FontWeight = FontWeights.SemiBold,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Cursor = Cursors.Hand
                };

                // Highlight today
                if (currentDay.Date == DateTime.Today)
                {
                    dayButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 220, 255));
                }

                // Hover effect
                dayButton.MouseEnter += (s, e) =>
                {
                    dayButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 240, 255));
                };
                dayButton.MouseLeave += (s, e) =>
                {
                    if (currentDay.Date == DateTime.Today)
                        dayButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 220, 255));
                    else
                        dayButton.Background = new SolidColorBrush(System.Windows.Media.Colors.White);
                };

                dayButton.Click += Button_Click;

                dayCounter++;
            }

            panelGrid.Children.Add(dayButton);
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button clickedButton && clickedButton.Tag is DateTime day)
        {
            MessageBox.Show($"You clicked: {day:MMMM dd, yyyy}");
            DayView sessionView = new DayView();
            if(sessionView.ShowDialog() == true)
            {
                
            }
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("TherapyTime v1.0", "About");
    }

}