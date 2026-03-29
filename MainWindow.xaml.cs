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
        } 
        else
        {
            //User closes the window
            Close(); //close the whole program.
        }
    }

    private void CreateButtonGrid()
    {
        panelGrid.Children.Clear(); // Clear existing buttons

        //DateTime to

        _buttonGrid = new Button[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                Button newButton = new Button
                {
                    Content = $"{i},{j}", // WPF uses Content, not Text
                    Tag = new System.Windows.Point(i, j),
                    Width = 30,
                    Height = 30,
                    Margin = new Thickness(2)
                };

                newButton.Click += Button_Click;

                // Add to the UniformGrid's children
                panelGrid.Children.Add(newButton);
                _buttonGrid[i, j] = newButton;
            }
        }
    }

    private void Button_Click(object sender, EventArgs e)
    {
        //verify not null and that it is a Button:
        if (sender is Button clickedButton && clickedButton.Tag is System.Windows.Point coords)
        {
            MessageBox.Show($"Button at Row: {coords.X}, Column: {coords.Y} clicked!");
        }
    }

}