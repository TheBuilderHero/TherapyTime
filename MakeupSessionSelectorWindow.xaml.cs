using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace TherapyTime;

public partial class MakeupSessionSelectorWindow : Window
{
    public Session? SelectedSession { get; private set; }

    public MakeupSessionSelectorWindow(Student student, EditDaySessionsWindow.EditableSession? currentMuSession = null)
    {
        InitializeComponent();

        var linkedNmSessionIdsByOthers = student.Sessions
            .Where(s => s.Code == SessionCode.MU && !string.IsNullOrWhiteSpace(s.LinkedSessionId) && s.Id != currentMuSession?.SessionId)
            .Select(s => s.LinkedSessionId!)
            .ToHashSet();

        var nmSessions = student.Sessions
            .Where(s => s.Code == SessionCode.NM && !linkedNmSessionIdsByOthers.Contains(s.Id))
            .OrderBy(s => s.SessionDateTime)
            .ToList();

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