using System.Drawing.Printing;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace TherapyTime;

/// <summary>
/// Manages a list of students and JSON persistence
/// </summary>
public static class StudentManager
{
    /// <summary>
    /// Load students from JSON string
    /// </summary>
    public static List<Student> LoadFromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<Student>>(json) ?? new List<Student>();
        }
        catch
        {
            return new List<Student>(); // fallback if JSON is corrupted
        }
    }

    /// <summary>
    /// Save students to JSON string
    /// </summary>
    public static string SaveToJson(List<Student> students) =>
        JsonSerializer.Serialize(students, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// Get students who have a session on a specific date
    /// </summary>
    public static List<Student> StudentsWithSessionOn(List<Student> students, DateTime date) =>
        students.Where(s => s.HasSessionOn(date)).ToList();
}



/// <summary>
    /// Represents a student and their therapy schedule
    /// </summary>
    public class Student
    {
        // Unique ID for the student
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Student name
        public string Name { get; set; } = string.Empty;

        // IEP therapy requirements
        public int MonthlyRequiredMinutes { get; set; } = 120;

        // Therapy sessions
        public List<Session> Sessions { get; set; } = new List<Session>();

        // Total minutes received and required
        public int TotalMinutesReceived { get; set; } = 0;
        public int TotalMinutesRequired { get; set; } = 120;

        // Annual reviews
        public List<DateTime> PastAnnualReviews { get; set; } = new List<DateTime>();
        public List<DateTime> FutureAnnualReviews { get; set; } = new List<DateTime>();

        // 3-year reevaluation
        public DateTime? NextThreeYearReevaluation { get; set; }

        // Constructor
        public Student(string name)
        {
            Name = name;
        }

        [JsonConstructor]
        public Student() { }

        /// <summary>
        /// Total therapy minutes in a given month/year
        /// </summary>
        public int TotalMinutesForMonth(int year, int month)
        {
            return Sessions
                    //.Concat(FutureSessions)
                    .Where(s => s.Date.Year == year && s.Date.Month == month)
                    .Sum(s => s.Minutes);
        }

        /// <summary>
        /// Check if student has any session on a given date
        /// </summary>
        public bool HasSessionOn(DateTime date)
        {
            var target = date.Date;
            return Sessions.Any(s => s.Date.Date == target);
        }

        /// <summary>
        /// Add a therapy session
        /// </summary>
        public void ScheduleSession(DateTime date, int minutes = 60, SessionCode code = SessionCode.IC)
        {
            //Prevent duplicates.
            if (Sessions.Any(s => s.Date == date))
                throw new InvalidOperationException("Session already exists for this date.");

            
            Sessions.Add(new Session(date, minutes, code));

            // Keep sessions sorted by date
            Sessions = Sessions.OrderBy(s => s.Date).ToList();
        }

        /// <summary>
        /// Add an annual review
        /// </summary>
        public void AddAnnualReview(DateTime date, bool isFuture = false)
        {
            if (isFuture)
                FutureAnnualReviews.Add(date);
            else
                PastAnnualReviews.Add(date);
        }
    }