using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace TherapyTime;

public partial class SessionNotesWindow : Window, INotifyPropertyChanged
{
    private bool _isReadOnly = false;
    
    public string Notes { get; private set; }
    
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set
        {
            if (_isReadOnly != value)
            {
                _isReadOnly = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SaveButtonVisibility));
            }
        }
    }
    
    public Visibility SaveButtonVisibility => IsReadOnly ? Visibility.Collapsed : Visibility.Visible;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SessionNotesWindow(string notes, bool isReadOnly = false)
    {
        InitializeComponent();
        Notes = notes;
        IsReadOnly = isReadOnly;
        NotesTextBox.Text = notes;
        NotesTextBox.DataContext = this;
        
        // Set the window DataContext for bindings
        DataContext = this;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Notes = NotesTextBox.Text;
        DialogResult = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void HelpBubble_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Session Notes Help\n\n" +
            "Purpose:\n" +
            "- Add or review notes tied to a specific session.\n\n" +
            "How to use this window:\n" +
            "- Enter notes and click Save when editing is allowed.\n" +
            "- Click Close to exit without changing notes.\n\n" +
            "What read-only means:\n" +
            "- Completed sessions may open notes in read-only mode to preserve finalized records.",
            "Session Notes Help",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}