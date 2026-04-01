using System.Windows;
using System.Windows.Controls;

namespace TherapyTime;

/// <summary>
/// Interaction logic for DayView.xaml
/// </summary>
public partial class DayView : Window {
    public DayView() {
        InitializeComponent(); //stitch it all together.

        DateTime date = DateTime.Now;
        List<Student> students = new List<Student>
        {
            new Student("kota"),
            new Student("bill")
        };
        students[0].scheduleSession(date, 50);
        students[1].scheduleSession(date, 60);
        List<Student> sessionsOnDate = StudentManager.StudentsWithSessionOn(students,date);

        userDataGrid.ItemsSource = sessionsOnDate;
    }


}