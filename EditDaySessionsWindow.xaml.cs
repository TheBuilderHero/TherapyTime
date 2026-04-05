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
    public class SessionCodeItem
    {
        public SessionCode Code { get; set; }
        public string DisplayText { get; set; } = string.Empty;

        public SessionCodeItem(SessionCode code, string description)
        {
            Code = code;
            DisplayText = $"{code} - {description}";
        }
    }

    public class EditableSession : INotifyPropertyChanged
    {
        private int _minutes;
        private SessionCode _code;
        private bool _isCompleted;

        public Student Student { get; set; } = null!;
        public List<SessionCodeItem> SessionCodeItems { get; } = new List<SessionCodeItem>
        {
            new SessionCodeItem(SessionCode.IC, "Incomplete - session has not yet taken place"),
            new SessionCodeItem(SessionCode.T, "Completed - session was successfully conducted"),
            new SessionCodeItem(SessionCode.NM, "Needs Makeup - session missed and requires makeup"),
            new SessionCodeItem(SessionCode.MU, "Makeup - replacement session for a missed one"),
            new SessionCodeItem(SessionCode.R, "Refused - session refused, no makeup needed"),
            new SessionCodeItem(SessionCode.A, "Absent - student absent, no makeup needed"),
            new SessionCodeItem(SessionCode.SU, "Unavailable - student unavailable, no makeup needed")
        };

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
        public string Notes { get; set; } = string.Empty;

        public EditableSession(Student student, int? minutes = null, SessionCode? code = null, bool isCompleted = false, DateTime? linkedSessionDate = null, bool isLinkedToMu = false, string notes = "")
        {
            Student = student;
            _minutes = minutes ?? 30;
            _code = code ?? SessionCode.IC;
            _isCompleted = isCompleted;
            LinkedSessionDate = linkedSessionDate;
            IsLinkedToMu = isLinkedToMu;
            Notes = notes;
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

    private List<Student> _allStudents;
    private DateTime _date;
    private DateTime _iepStartDate;
    private DateTime _iepEndDate;
    private List<EditableSession> _editableSessions;
    private bool _hasUnsavedChanges = false;

    private class StudentSnapshot
    {
        public Student Student { get; set; } = null!;
        public int TotalMinutesReceived { get; set; }
        public int TotalMinutesRequired { get; set; }
        public List<Session> SessionsOnDate { get; set; } = new List<Session>();
    }

    private List<StudentSnapshot> _originalStudentSnapshots = new List<StudentSnapshot>();

    public EditDaySessionsWindow(List<Student> students, DateTime date, DateTime iepStartDate, DateTime iepEndDate)
    {
        InitializeComponent();

        _allStudents = students;
        _date = date;
        _iepStartDate = iepStartDate;
        _iepEndDate = iepEndDate;

        // Capture original state so unsaved changes can be discarded
        _originalStudentSnapshots = students.Select(s => new StudentSnapshot
        {
            Student = s,
            TotalMinutesReceived = s.TotalMinutesReceived,
            TotalMinutesRequired = s.TotalMinutesRequired,
            SessionsOnDate = s.Sessions
                .Where(sess => sess.Date.Date == date.Date)
                .Select(sess => new Session(sess.Date, sess.Minutes, sess.Code)
                {
                    IsCompleted = sess.IsCompleted,
                    LinkedSessionDate = sess.LinkedSessionDate,
                    Notes = sess.Notes
                })
                .ToList()
        }).ToList();

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
                    session.Code == SessionCode.NM && s.Sessions.Any(mu => mu.Code == SessionCode.MU && mu.LinkedSessionDate.HasValue && mu.LinkedSessionDate.Value.Date == session.Date.Date),
                    session.Notes
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
                LinkedSessionDate = es.LinkedSessionDate,
                Notes = es.Notes
            });
        }

        _hasUnsavedChanges = false;
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

            // If completing the session, prompt for notes
            if (!row.IsCompleted)
            {
                var notesWindow = new SessionNotesWindow(row.Notes, false)
                {
                    Owner = this,
                    Title = $"Add Notes for {row.Student.Name} - {_date:MM/dd/yyyy}"
                };

                if (notesWindow.ShowDialog() == true)
                {
                    row.Notes = notesWindow.Notes;
                }
                // Note: We don't return here - the session will still be completed even if notes are cancelled
            }

            row.ToggleCompletion();
            _hasUnsavedChanges = true;
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

    private void SessionsGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
        _hasUnsavedChanges = true;
    }

    private void AddSession_Click(object sender, RoutedEventArgs e)
    {
        // Open AddSessionWindow with the current date pre-selected
        AddSessionWindow addSessionWindow = new AddSessionWindow(_allStudents, _iepStartDate, _iepEndDate, _date, true)
        {
            Owner = this
        };

        if (addSessionWindow.ShowDialog() == true)
        {
            var student = addSessionWindow.SelectedStudent!;
            var date = addSessionWindow.SelectedDate;
            int minutes = addSessionWindow.Minutes;
            SessionCode sessionCode = addSessionWindow.SessionCode;

            if (!student.HasSessionOn(date))
            {
                student.ScheduleSession(date, minutes, sessionCode);
                // Note: We don't call SaveStudents() here because the user might make additional changes
                // in the EditDaySessionsWindow before saving. The Save_Click method will handle persistence.
                
                MessageBox.Show($"Session added for {student.Name} on {date:MM/dd/yyyy} ({minutes} minutes, code: {sessionCode}).", "Info");
                
                // Refresh the DataGrid to show the newly added session
                RefreshDataGrid();
                _hasUnsavedChanges = true;
            }
            else
            {
                MessageBox.Show($"{student.Name} already has a session on {date:MM/dd/yyyy}.", "Info");
            }
        }
    }

    private void DeleteSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is EditableSession row)
        {
            // Check for linked sessions
            string warningMessage = "";
            if (row.Code == SessionCode.NM)
            {
                var linkedMu = row.Student.Sessions.FirstOrDefault(s => s.Code == SessionCode.MU && s.LinkedSessionDate.HasValue && s.LinkedSessionDate.Value.Date == _date.Date);
                if (linkedMu != null)
                {
                    warningMessage = $"\n\nWarning: This session has a linked makeup session on {linkedMu.Date:MM/dd/yyyy}. Deleting this session will break the link.";
                }
            }
            else if (row.Code == SessionCode.MU && row.LinkedSessionDate.HasValue)
            {
                var linkedNm = row.Student.Sessions.FirstOrDefault(s => s.Code == SessionCode.NM && s.Date.Date == row.LinkedSessionDate.Value.Date);
                if (linkedNm != null)
                {
                    warningMessage = $"\n\nWarning: This makeup session is linked to a missed session on {row.LinkedSessionDate.Value:MM/dd/yyyy}. Deleting this session will break the link.";
                }
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the session for {row.Student.Name} on {_date:MM/dd/yyyy}?{warningMessage}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Remove the session from the student's sessions list
                row.Student.Sessions.RemoveAll(s => s.Date.Date == _date.Date);

                // If the session was completed, we need to undo the minute adjustments
                if (row.IsCompleted)
                {
                    // Undo the action based on Code
                    switch (row.Code)
                    {
                        case SessionCode.T:
                        case SessionCode.IC:
                            row.Student.TotalMinutesReceived -= row.Minutes;
                            break;
                        case SessionCode.MU:
                            row.Student.TotalMinutesReceived -= row.Minutes;
                            break;
                        case SessionCode.R:
                        case SessionCode.A:
                        case SessionCode.SU:
                            row.Student.TotalMinutesRequired += row.Minutes;
                            break;
                        case SessionCode.NM:
                            // No action for placeholders
                            break;
                    }
                }

                // Refresh the DataGrid to reflect the deletion
                RefreshDataGrid();
                _hasUnsavedChanges = true;
            }
        }
    }

    private void NotesSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is EditableSession row)
        {
            // Open notes window - allow editing only if session is NOT completed
            bool canEdit = !row.IsCompleted;
            var notesWindow = new SessionNotesWindow(row.Notes, !canEdit)
            {
                Owner = this
            };

            if (notesWindow.ShowDialog() == true)
            {
                // Save the notes back to the editable session
                row.Notes = notesWindow.Notes;
                _hasUnsavedChanges = true;
            }
        }
    }

    private void RefreshDataGrid()
    {
        // Rebuild the list of editable sessions
        _editableSessions = _allStudents
            .Select(s =>
            {
                var session = s.Sessions.FirstOrDefault(sess => sess.Date.Date == _date.Date);
                if (session == null)
                    return null;

                return new EditableSession(
                    s,
                    session.Minutes,
                    session.Code,
                    session.IsCompleted,
                    session.LinkedSessionDate,
                    session.Code == SessionCode.NM && s.Sessions.Any(mu => mu.Code == SessionCode.MU && mu.LinkedSessionDate.HasValue && mu.LinkedSessionDate.Value.Date == session.Date.Date),
                    session.Notes
                );
            })
            .Where(es => es != null)
            .Select(es => es!)
            .ToList();

        // Re-bind the list to the DataGrid
        SessionsGrid.ItemsSource = null;
        SessionsGrid.ItemsSource = _editableSessions;
    }

    private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        T parent = parentObject as T;
        if (parent != null) return parent;
        return FindVisualParent<T>(parentObject);
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_hasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. If you continue, all changes will be lost.\n\nDo you want to continue without saving?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true; // Prevent the window from closing
            }
            else
            {
                RestoreOriginalState();
            }
        }
    }

    private void CancelChanges_Click(object sender, RoutedEventArgs e)
    {
        if (_hasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. If you continue, all changes will be lost.\n\nDo you want to continue without saving?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                RestoreOriginalState();
                _hasUnsavedChanges = false;
                DialogResult = false; // Close without saving
            }
        }
        else
        {
            DialogResult = false; // Close without saving
        }
    }

    private void RestoreOriginalState()
    {
        foreach (var snapshot in _originalStudentSnapshots)
        {
            var student = snapshot.Student;
            student.TotalMinutesReceived = snapshot.TotalMinutesReceived;
            student.TotalMinutesRequired = snapshot.TotalMinutesRequired;

            student.Sessions.RemoveAll(s => s.Date.Date == _date.Date);
            student.Sessions.AddRange(snapshot.SessionsOnDate.Select(sess => new Session(sess.Date, sess.Minutes, sess.Code)
            {
                IsCompleted = sess.IsCompleted,
                LinkedSessionDate = sess.LinkedSessionDate,
                Notes = sess.Notes
            }));
            student.Sessions = student.Sessions.OrderBy(s => s.Date).ToList();
        }
    }
}

