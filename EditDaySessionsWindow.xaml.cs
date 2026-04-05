using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TherapyTime;

public partial class EditDaySessionsWindow : Window
{
    public class EditableSession : INotifyPropertyChanged
    {
        private int _minutes;
        private SessionCode _code;
        private bool _isCompleted;

        public Student Student { get; set; } = null!;
        public List<SessionCode> SessionCodes { get; } = Enum.GetValues(typeof(SessionCode)).Cast<SessionCode>().ToList();

        public int Minutes
        {
            get => _minutes;
            set
            {
                if (_minutes != value)
                {
                    _minutes = value;
                    OnPropertyChanged();
                }
            }
        }

        public SessionCode Code
        {
            get => _code;
            set
            {
                if (_code != value)
                {
                    _code = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLinkedToMu { get; set; }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ActionButtonText));
                }
            }
        }

        public string ActionButtonText => IsCompleted ? "Change" : "Complete Session";

        public event PropertyChangedEventHandler? PropertyChanged;

        public DateTime? LinkedSessionDate { get; set; }

        public EditableSession(Student student, int? minutes = null, SessionCode? code = null, bool isCompleted = false, DateTime? linkedSessionDate = null, bool isLinkedToMu = false)
        {
            Student = student;
            _minutes = minutes ?? 30;
            _code = code ?? SessionCode.IC;
            _isCompleted = isCompleted;
            LinkedSessionDate = linkedSessionDate;
            IsLinkedToMu = isLinkedToMu;
        }

        public void ToggleCompletion()
        {
            if (IsCompleted)
            {
                IsCompleted = false;
                // Undo the action based on Code
                switch (Code)
                {
                    case SessionCode.T:
                    case SessionCode.IC:
                        Student.TotalMinutesReceived -= Minutes;
                        break;
                    case SessionCode.MU:
                        Student.TotalMinutesReceived -= Minutes;
                        break;
                    case SessionCode.R:
                    case SessionCode.A:
                    case SessionCode.SU:
                        Student.TotalMinutesRequired += Minutes;
                        break;
                    case SessionCode.NM:
                        // No action for placeholders
                        break;
                }
            }
            else
            {
                IsCompleted = true;
                // Perform action based on Code
                switch (Code)
                {
                    case SessionCode.T:
                    case SessionCode.IC:
                        Student.TotalMinutesReceived += Minutes;
                        break;
                    case SessionCode.MU:
                        Student.TotalMinutesReceived += Minutes;
                        break;
                    case SessionCode.R:
                    case SessionCode.A:
                    case SessionCode.SU:
                        Student.TotalMinutesRequired -= Minutes;
                        break;
                    case SessionCode.NM:
                        // No action for placeholders
                        break;
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    private DateTime _date;
    private List<EditableSession> _editableSessions;

    public EditDaySessionsWindow(List<Student> students, DateTime date)
    {
        InitializeComponent();

        _date = date;

        // Build the list of editable sessions for the DataGrid
        _editableSessions = students
            .Select(s =>
            {
                var session = s.Sessions.FirstOrDefault(sess => sess.Date.Date == date.Date);
                if (session == null)
                    return null;

                return new EditableSession(
                    s,
                    session.Minutes,
                    session.Code,
                    session.IsCompleted,
                    session.LinkedSessionDate,
                    session.Code == SessionCode.NM && s.Sessions.Any(mu => mu.Code == SessionCode.MU && mu.LinkedSessionDate.HasValue && mu.LinkedSessionDate.Value.Date == session.Date.Date)
                );
            })
            .Where(es => es != null)
            .Select(es => es!)
            .ToList();

        // Bind the list to the DataGrid
        SessionsGrid.ItemsSource = _editableSessions;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        foreach (var es in _editableSessions)
        {
            // Remove existing session on this date
            es.Student.Sessions.RemoveAll(s => s.Date.Date == _date.Date);

            // Add new/updated session
            es.Student.Sessions.Add(new Session(_date, es.Minutes, es.Code)
            {
                IsCompleted = es.IsCompleted,
                LinkedSessionDate = es.LinkedSessionDate
            });
        }

        DialogResult = true; // close window
    }

    private void ToggleComplete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is EditableSession row)
        {
            // Commit any pending edits in the DataGrid
            SessionsGrid.CommitEdit(DataGridEditingUnit.Row, true);

            if (row.Code == SessionCode.IC)
            {
                MessageBox.Show("You cannot complete a session with code IC. Please select a different session code first.", "Invalid Session Code", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!row.IsCompleted && row.Code == SessionCode.MU)
            {
                // Open popup to select NM session for linking (no change to NM)
                var selector = new MakeupSessionSelectorWindow(row.Student, row);
                if (selector.ShowDialog() == true && selector.SelectedSession != null)
                {
                    row.LinkedSessionDate = selector.SelectedSession.Date;
                }
                else
                {
                    return; // Do not complete MU session without a linked NM session
                }
            }
            row.ToggleCompletion();
        }
    }

    private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is TextBlock tb && tb.DataContext is EditableSession)
        {
            var cell = FindVisualParent<DataGridCell>(tb);
            if (cell != null && !cell.IsEditing)
            {
                SessionsGrid.BeginEdit();
                // Do not set e.Handled = true to allow the ComboBox to receive the click
            }
        }
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null && !(parent is T))
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        return parent as T;
    }
}