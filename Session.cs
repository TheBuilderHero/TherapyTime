namespace TherapyTime;

public class Session
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Date { get; set; }
    public TimeSpan TimeOfDay { get; set; } = new TimeSpan(8, 0, 0);
    public int Minutes { get; set; }
    public SessionCode Code { get; set; }  // enum now
    public bool IsCompleted { get; set; } = false;
    public string? LinkedSessionId { get; set; }
    public DateTime? LinkedSessionDate { get; set; }
    public string Notes { get; set; } = string.Empty;

    public DateTime SessionDateTime => Date.Date + TimeOfDay;

    public string DisplayText => $"{Date:MM/dd/yyyy} {DateTime.Today.Add(TimeOfDay):hh:mm tt} - {Minutes} minutes";

    public Session(DateTime date, int minutes, SessionCode code, TimeSpan? timeOfDay = null)
    {
        Date = date.Date;
        TimeOfDay = timeOfDay ?? new TimeSpan(8, 0, 0);
        Minutes = minutes;
        Code = code;
    }
}