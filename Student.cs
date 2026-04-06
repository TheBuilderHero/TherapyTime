using System.Drawing.Printing;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace TherapyTime;

/// <summary>
/// Holds core student data that persists across IEP date ranges
/// </summary>
public class StudentCoreData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsArchived { get; set; } = false;
    public int MonthlyRequiredMinutes { get; set; } = 120;
    public List<DateTime> PastAnnualReviews { get; set; } = new List<DateTime>();
    public List<DateTime> FutureAnnualReviews { get; set; } = new List<DateTime>();
    public DateTime? NextThreeYearReevaluation { get; set; }
}

/// <summary>
/// Holds IEP-specific data for a student
/// </summary>
public class StudentIepData
{
    public string Id { get; set; } = string.Empty;
    public List<Session> Sessions { get; set; } = new List<Session>();
    public int TotalMinutesReceived { get; set; } = 0;
    public int TotalMinutesRequired { get; set; } = 120;
}

/// <summary>
/// Manages a list of students and JSON persistence
/// </summary>
public static class StudentManager
{
    private static readonly JsonSerializerOptions JsonReadOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Load students from JSON string (legacy full format)
    /// </summary>
    public static List<Student> LoadFromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<Student>>(json, JsonReadOptions) ?? new List<Student>();
        }
        catch
        {
            return new List<Student>(); // fallback if JSON is corrupted
        }
    }

    /// <summary>
    /// Save students to JSON string (legacy full format)
    /// </summary>
    public static string SaveToJson(List<Student> students) =>
        JsonSerializer.Serialize(students, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// Extract and save core student data to JSON
    /// </summary>
    public static string SaveCoreDataToJson(List<Student> students)
    {
        var coreData = students.Select(s => new StudentCoreData
        {
            Id = s.Id,
            Name = s.Name,
            IsArchived = s.IsArchived,
            MonthlyRequiredMinutes = s.MonthlyRequiredMinutes,
            PastAnnualReviews = s.PastAnnualReviews,
            FutureAnnualReviews = s.FutureAnnualReviews,
            NextThreeYearReevaluation = s.NextThreeYearReevaluation
        }).ToList();

        return JsonSerializer.Serialize(coreData, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Extract and save IEP-specific data to JSON
    /// </summary>
    public static string SaveIepDataToJson(List<Student> students)
    {
        var iepData = students.Select(s => new StudentIepData
        {
            Id = s.Id,
            Sessions = s.Sessions,
            TotalMinutesReceived = s.TotalMinutesReceived,
            TotalMinutesRequired = s.TotalMinutesRequired
        }).ToList();

        return JsonSerializer.Serialize(iepData, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Load core student data from JSON
    /// </summary>
    public static List<StudentCoreData> LoadCoreDataFromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<StudentCoreData>>(json, JsonReadOptions) ?? new List<StudentCoreData>();
        }
        catch
        {
            return new List<StudentCoreData>();
        }
    }

    /// <summary>
    /// Load IEP-specific data from JSON
    /// </summary>
    public static List<StudentIepData> LoadIepDataFromJson(string json)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<List<StudentIepData>>(json, JsonReadOptions);
            if (parsed != null)
            {
                return parsed;
            }

            return new List<StudentIepData>();
        }
        catch
        {
            // Fallback parser for legacy/partially incompatible session payloads.
            try
            {
                var result = new List<StudentIepData>();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return result;
                }

                foreach (var studentElem in doc.RootElement.EnumerateArray())
                {
                    string id = studentElem.TryGetProperty("Id", out var idElem) ? (idElem.GetString() ?? string.Empty) : string.Empty;
                    int totalReceived = studentElem.TryGetProperty("TotalMinutesReceived", out var recvElem) && recvElem.TryGetInt32(out int recv) ? recv : 0;
                    int totalRequired = studentElem.TryGetProperty("TotalMinutesRequired", out var reqElem) && reqElem.TryGetInt32(out int req) ? req : 120;

                    var iep = new StudentIepData
                    {
                        Id = id,
                        TotalMinutesReceived = totalReceived,
                        TotalMinutesRequired = totalRequired,
                        Sessions = new List<Session>()
                    };

                    if (studentElem.TryGetProperty("Sessions", out var sessionsElem) && sessionsElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var sessElem in sessionsElem.EnumerateArray())
                        {
                            var session = new Session();

                            if (sessElem.TryGetProperty("Id", out var sessIdElem))
                                session.Id = sessIdElem.GetString() ?? Guid.NewGuid().ToString();

                            if (sessElem.TryGetProperty("Date", out var dateElem) && DateTime.TryParse(dateElem.GetString(), out DateTime parsedDate))
                                session.Date = parsedDate.Date;

                            if (sessElem.TryGetProperty("TimeOfDay", out var timeElem) && TimeSpan.TryParse(timeElem.GetString(), out TimeSpan parsedTime))
                                session.TimeOfDay = parsedTime;

                            if (sessElem.TryGetProperty("Minutes", out var minutesElem) && minutesElem.TryGetInt32(out int minutes))
                                session.Minutes = minutes;

                            if (sessElem.TryGetProperty("Code", out var codeElem) && codeElem.TryGetInt32(out int codeValue))
                                session.Code = (SessionCode)codeValue;

                            if (sessElem.TryGetProperty("IsCompleted", out var completedElem) && completedElem.ValueKind is JsonValueKind.True or JsonValueKind.False)
                                session.IsCompleted = completedElem.GetBoolean();

                            if (sessElem.TryGetProperty("LinkedSessionId", out var linkedIdElem))
                                session.LinkedSessionId = linkedIdElem.GetString();

                            if (sessElem.TryGetProperty("LinkedSessionDate", out var linkedDateElem) && DateTime.TryParse(linkedDateElem.GetString(), out DateTime linkedDate))
                                session.LinkedSessionDate = linkedDate;

                            if (sessElem.TryGetProperty("Notes", out var notesElem))
                                session.Notes = notesElem.GetString() ?? string.Empty;

                            iep.Sessions.Add(session);
                        }
                    }

                    result.Add(iep);
                }

                return result;
            }
            catch
            {
                return new List<StudentIepData>();
            }
        }
    }

    /// <summary>
    /// Merge core student data with IEP-specific data
    /// </summary>
    public static List<Student> MergeStudentData(List<StudentCoreData> coreData, List<StudentIepData> iepData)
    {
        var result = new List<Student>();

        // Create students from core data
        foreach (var core in coreData)
        {
            var student = new Student
            {
                Id = core.Id,
                Name = core.Name,
                IsArchived = core.IsArchived,
                MonthlyRequiredMinutes = core.MonthlyRequiredMinutes,
                PastAnnualReviews = core.PastAnnualReviews,
                FutureAnnualReviews = core.FutureAnnualReviews,
                NextThreeYearReevaluation = core.NextThreeYearReevaluation,
                Sessions = new List<Session>(),
                TotalMinutesReceived = 0,
                TotalMinutesRequired = core.MonthlyRequiredMinutes
            };

            // Overlay IEP-specific data
            var iepInfo = iepData.FirstOrDefault(i => i.Id == student.Id);
            if (iepInfo != null)
            {
                student.Sessions = iepInfo.Sessions;
                student.TotalMinutesReceived = iepInfo.TotalMinutesReceived;
                student.TotalMinutesRequired = iepInfo.TotalMinutesRequired;
            }

            result.Add(student);
        }

        // Include IEP-only students if core data is missing or incomplete.
        foreach (var iepOnly in iepData.Where(i => !result.Any(r => r.Id == i.Id)))
        {
            result.Add(new Student
            {
                Id = iepOnly.Id,
                Name = string.IsNullOrWhiteSpace(iepOnly.Id) ? "Unknown Student" : $"Student {iepOnly.Id[..Math.Min(8, iepOnly.Id.Length)]}",
                IsArchived = false,
                MonthlyRequiredMinutes = 120,
                Sessions = iepOnly.Sessions ?? new List<Session>(),
                TotalMinutesReceived = iepOnly.TotalMinutesReceived,
                TotalMinutesRequired = iepOnly.TotalMinutesRequired
            });
        }

        return result;
    }

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

        // Hidden from session creation lists when archived
        public bool IsArchived { get; set; } = false;

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
        public Session ScheduleSession(DateTime date, int minutes = 60, SessionCode code = SessionCode.IC, TimeSpan? timeOfDay = null)
        {
            var session = new Session(date, minutes, code, timeOfDay);
            Sessions.Add(session);

            // Keep sessions sorted by date
            Sessions = Sessions
                .OrderBy(s => s.SessionDateTime)
                .ThenBy(s => s.Id)
                .ToList();

            return session;
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