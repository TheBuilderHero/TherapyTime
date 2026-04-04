using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace TherapyTime;

public partial class MakeupSessionSelectorWindow : Window
{
    public Session? SelectedSession { get; private set; }

    public MakeupSessionSelectorWindow(Student student)
    {
        InitializeComponent();
        var nmSessions = student.Sessions.Where(s => s.Code == SessionCode.NM).ToList();
        SessionsList.ItemsSource = nmSessions;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        SelectedSession = SessionsList.SelectedItem as Session;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}