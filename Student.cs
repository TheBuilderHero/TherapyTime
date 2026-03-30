using System.Text.Json;
using System.Text.Json.Serialization;


namespace TherapyTime.Models;

/// <summary>
/// Manages a list of students and JSON persistence
/// </summary>
public static class StudentManager
{
    /// <summary>
    /// Load students from JSON string
    /// </summary>
    public static List<Student> LoadFromJson(string json) =>
        JsonSerializer.Deserialize<List<Student>>(json) ?? new List<Student>();

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
        public List<TherapySession> PastSessions { get; set; } = new List<TherapySession>();
        public List<TherapySession> FutureSessions { get; set; } = new List<TherapySession>();

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
            return PastSessions
                    .Concat(FutureSessions)
                    .Where(s => s.Date.Year == year && s.Date.Month == month)
                    .Sum(s => s.Minutes);
        }

        /// <summary>
        /// Check if student has any session on a given date
        /// </summary>
        public bool HasSessionOn(DateTime date)
        {
            return PastSessions.Any(s => s.Date.Date == date.Date)
                || FutureSessions.Any(s => s.Date.Date == date.Date);
        }

        /// <summary>
        /// Add a therapy session
        /// </summary>
        public void AddSession(DateTime date, int minutes = 60, bool isFuture = false, SessionCode status = SessionCode.T, string? notes = null)
        {
            // Only allow notes if session code is NM or R
            if (status != SessionCode.NM && status != SessionCode.R)
                notes = null;

            var session = new TherapySession(date, minutes, status, notes);

            if (isFuture)
                FutureSessions.Add(session);
            else
                PastSessions.Add(session);
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