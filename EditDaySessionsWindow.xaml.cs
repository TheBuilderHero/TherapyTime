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
        private TimeSpan _timeOfDay;

        public Student Student { get; set; } = null!;
        public string SessionId { get; set; } = string.Empty;
        public List<TimeSpan> TimeItems { get; } = BuildTimeItems();
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

        public TimeSpan TimeOfDay
        {
            get => _timeOfDay;
            set
            {
                if (_timeOfDay != value)
                {
                    _timeOfDay = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TimeDisplay));
                }
            }
        }

        public string TimeDisplay => DateTime.Today.Add(TimeOfDay).ToString("hh:mm tt");

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

        public string? LinkedSessionId { get; set; }
        public DateTime? LinkedSessionDate { get; set; }
        public string Notes { get; set; } = string.Empty;

        public EditableSession(string sessionId, Student student, TimeSpan timeOfDay, int? minutes = null, SessionCode? code = null, bool isCompleted = false, string? linkedSessionId = null, DateTime? linkedSessionDate = null, bool isLinkedToMu = false, string notes = "")
        {
            SessionId = sessionId;
            Student = student;
            _timeOfDay = timeOfDay;
            _minutes = minutes ?? 30;
            _code = code ?? SessionCode.IC;
            _isCompleted = isCompleted;
            LinkedSessionId = linkedSessionId;
            LinkedSessionDate = linkedSessionDate;
            IsLinkedToMu = isLinkedToMu;
            Notes = notes;
        }

        private static List<TimeSpan> BuildTimeItems()
        {
            var result = new List<TimeSpan>();
            for (int hour = 7; hour <= 18; hour++)
            {
                for (int minute = 0; minute < 60; minute += 15)
                {
                    result.Add(new TimeSpan(hour, minute, 0));
                }
            }

            return result;
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
    private List<Student> _studentsAvailableForAdd;
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

    public EditDaySessionsWindow(List<Student> students, List<Student> studentsAvailableForAdd, DateTime date, DateTime iepStartDate, DateTime iepEndDate)
    {
        InitializeComponent();

        _allStudents = students;
        _studentsAvailableForAdd = studentsAvailableForAdd;
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
                    Id = sess.Id,
                    TimeOfDay = sess.TimeOfDay,
                    IsCompleted = sess.IsCompleted,
                    LinkedSessionId = sess.LinkedSessionId,
                    LinkedSessionDate = sess.LinkedSessionDate,
                    Notes = sess.Notes
                })
                .ToList()
        }).ToList();

        // Build the list of editable sessions for the DataGrid
        _editableSessions = students
            .SelectMany(s => s.Sessions
                .Where(sess => sess.Date.Date == date.Date)
                .Select(sess => new EditableSession(
                    sess.Id,
                    s,
                    sess.TimeOfDay,
                    sess.Minutes,
                    sess.Code,
                    sess.IsCompleted,
                    sess.LinkedSessionId,
                    sess.LinkedSessionDate,
                    sess.Code == SessionCode.NM && s.Sessions.Any(mu => mu.Code == SessionCode.MU && mu.LinkedSessionId == sess.Id),
                    sess.Notes
                )))
            .OrderBy(es => es.TimeOfDay)
            .ThenBy(es => es.Student.Name)
            .ToList();

        // Bind the list to the DataGrid
        SessionsGrid.ItemsSource = _editableSessions;
    }

    private bool SaveSessionsCore()
    {
        // Commit any active edit before validating/saving.
        SessionsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        SessionsGrid.CommitEdit(DataGridEditingUnit.Row, true);

        // Validate no overlapping sessions among pending edits for the entire day
        var sorted = _editableSessions
            .OrderBy(es => es.TimeOfDay)
            .ThenBy(es => es.Student.Name)
            .ToList();

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            var a = sorted[i];
            var b = sorted[i + 1];
            var aEnd = a.TimeOfDay.Add(TimeSpan.FromMinutes(a.Minutes));
            if (b.TimeOfDay < aEnd)
            {
                MessageBox.Show(
                    "The following sessions overlap on this day:\n" +
                    $"  {a.Student.Name}: {DateTime.Today.Add(a.TimeOfDay):hh:mm tt} - {a.Minutes} min ({a.Code})\n" +
                    $"  {b.Student.Name}: {DateTime.Today.Add(b.TimeOfDay):hh:mm tt} - {b.Minutes} min ({b.Code})\n\n" +
                    "Please adjust session times before saving.",
                    "Session Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        foreach (var es in _editableSessions)
        {
            var matchingSession = es.Student.Sessions.FirstOrDefault(s => s.Id == es.SessionId);
            if (matchingSession == null)
                continue;

            matchingSession.Date = _date.Date;
            matchingSession.TimeOfDay = es.TimeOfDay;
            matchingSession.Minutes = es.Minutes;
            matchingSession.Code = es.Code;
            matchingSession.IsCompleted = es.IsCompleted;
            matchingSession.LinkedSessionId = es.LinkedSessionId;
            matchingSession.LinkedSessionDate = es.LinkedSessionDate;
            matchingSession.Notes = es.Notes;
        }

        foreach (var student in _allStudents)
        {
            student.Sessions = student.Sessions
                .OrderBy(s => s.SessionDateTime)
                .ThenBy(s => s.Id)
                .ToList();
        }

        return true;
    }

    private void CaptureCurrentStateAsOriginal()
    {
        _originalStudentSnapshots = _allStudents.Select(s => new StudentSnapshot
        {
            Student = s,
            TotalMinutesReceived = s.TotalMinutesReceived,
            TotalMinutesRequired = s.TotalMinutesRequired,
            SessionsOnDate = s.Sessions
                .Where(sess => sess.Date.Date == _date.Date)
                .Select(sess => new Session(sess.Date, sess.Minutes, sess.Code)
                {
                    Id = sess.Id,
                    TimeOfDay = sess.TimeOfDay,
                    IsCompleted = sess.IsCompleted,
                    LinkedSessionId = sess.LinkedSessionId,
                    LinkedSessionDate = sess.LinkedSessionDate,
                    Notes = sess.Notes
                })
                .ToList()
        }).ToList();
    }

    private void SaveSessions_Click(object sender, RoutedEventArgs e)
    {
        if (!SaveSessionsCore())
        {
            return;
        }

        _hasUnsavedChanges = false;
        CaptureCurrentStateAsOriginal();
        MessageBox.Show("Sessions saved. You can continue editing.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!SaveSessionsCore())
        {
            return;
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
                if (_hasUnsavedChanges)
                {
                    var warningResult = MessageBox.Show(
                        "You have unsaved changes. Until you save, newly edited or added NM sessions may not appear in the makeup-session link list.\n\nSave your changes first for the most up-to-date list.\n\nContinue anyway?",
                        "Unsaved Changes",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (warningResult == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                // Open popup to select NM session for linking (no change to NM)
                var selector = new MakeupSessionSelectorWindow(row.Student, row);
                if (selector.ShowDialog() == true && selector.SelectedSession != null)
                {
                    row.LinkedSessionId = selector.SelectedSession.Id;
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
        AddSessionWindow addSessionWindow = new AddSessionWindow(_studentsAvailableForAdd, _iepStartDate, _iepEndDate, _date, true)
        {
            Owner = this
        };

        if (addSessionWindow.ShowDialog() == true)
        {
            var student = addSessionWindow.SelectedStudent!;
            var date = addSessionWindow.SelectedDate;
            int minutes = addSessionWindow.Minutes;
            SessionCode sessionCode = addSessionWindow.SessionCode;
            TimeSpan sessionTime = addSessionWindow.SessionTime;

            // Check for overlapping sessions across all students on the selected date
            var newStart = sessionTime;
            var newEnd = newStart.Add(TimeSpan.FromMinutes(minutes));
            var conflict = _allStudents
                .SelectMany(s => s.Sessions
                    .Where(sess => sess.Date.Date == date.Date)
                    .Select(sess => new { Student = s, Session = sess }))
                .FirstOrDefault(x =>
                {
                    var existingEnd = x.Session.TimeOfDay.Add(TimeSpan.FromMinutes(x.Session.Minutes));
                    return newStart < existingEnd && newEnd > x.Session.TimeOfDay;
                });
            if (conflict != null)
            {
                MessageBox.Show(
                    $"This time conflicts with an existing session for {conflict.Student.Name}:\n" +
                    $"{DateTime.Today.Add(conflict.Session.TimeOfDay):hh:mm tt} - {conflict.Session.Minutes} min ({conflict.Session.Code})",
                    "Session Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            student.ScheduleSession(date, minutes, sessionCode, sessionTime);
            // Note: We don't call SaveStudents() here because the user might make additional changes
            // in the EditDaySessionsWindow before saving. The Save_Click method will handle persistence.

            MessageBox.Show($"Session added for {student.Name} on {date:MM/dd/yyyy} at {DateTime.Today.Add(sessionTime):hh:mm tt} ({minutes} minutes, code: {sessionCode}).", "Info");

            // Refresh the DataGrid to show the newly added session
            RefreshDataGrid();
            _hasUnsavedChanges = true;
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
                var linkedMu = row.Student.Sessions.Where(s => s.Code == SessionCode.MU && s.LinkedSessionId == row.SessionId).ToList();
                if (linkedMu != null)
                {
                    if (linkedMu.Count > 0)
                    {
                        warningMessage = "\n\nWarning: This session has linked makeup session(s). Deleting this session will break those links.";
                    }
                }
            }
            else if (row.Code == SessionCode.MU && !string.IsNullOrWhiteSpace(row.LinkedSessionId))
            {
                var linkedNm = row.Student.Sessions.FirstOrDefault(s => s.Code == SessionCode.NM && s.Id == row.LinkedSessionId);
                if (linkedNm != null)
                {
                    warningMessage = $"\n\nWarning: This makeup session is linked to a missed session on {linkedNm.Date:MM/dd/yyyy} at {DateTime.Today.Add(linkedNm.TimeOfDay):hh:mm tt}. Deleting this session will break the link.";
                }
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the session for {row.Student.Name} on {_date:MM/dd/yyyy}?{warningMessage}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var rowSession = row.Student.Sessions.FirstOrDefault(s => s.Id == row.SessionId);
                if (rowSession == null)
                {
                    RefreshDataGrid();
                    return;
                }

                // Remove the session from the student's sessions list
                row.Student.Sessions.RemoveAll(s => s.Id == row.SessionId);

                // If deleting an NM session, unlink any MU sessions connected to it
                foreach (var mu in row.Student.Sessions.Where(s => s.Code == SessionCode.MU && s.LinkedSessionId == row.SessionId))
                {
                    mu.LinkedSessionId = null;
                    mu.LinkedSessionDate = null;
                }

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
            .SelectMany(s => s.Sessions
                .Where(sess => sess.Date.Date == _date.Date)
                .Select(sess => new EditableSession(
                    sess.Id,
                    s,
                    sess.TimeOfDay,
                    sess.Minutes,
                    sess.Code,
                    sess.IsCompleted,
                    sess.LinkedSessionId,
                    sess.LinkedSessionDate,
                    sess.Code == SessionCode.NM && s.Sessions.Any(mu => mu.Code == SessionCode.MU && mu.LinkedSessionId == sess.Id),
                    sess.Notes
                )))
            .OrderBy(es => es.TimeOfDay)
            .ThenBy(es => es.Student.Name)
            .ToList();

        // Re-bind the list to the DataGrid
        SessionsGrid.ItemsSource = null;
        SessionsGrid.ItemsSource = _editableSessions;
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        T? parent = parentObject as T;
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
                Id = sess.Id,
                TimeOfDay = sess.TimeOfDay,
                IsCompleted = sess.IsCompleted,
                LinkedSessionId = sess.LinkedSessionId,
                LinkedSessionDate = sess.LinkedSessionDate,
                Notes = sess.Notes
            }));
            student.Sessions = student.Sessions.OrderBy(s => s.SessionDateTime).ThenBy(s => s.Id).ToList();
        }
    }
}

