namespace TherapyTime;

public class Session
{
    public DateTime Date { get; set; }
    public int Minutes { get; set; }
    public SessionCode Code { get; set; }  // enum now
    public bool IsCompleted { get; set; } = false;

    public Session(DateTime date, int minutes, SessionCode code)
    {
        Date = date;
        Minutes = minutes;
        Code = code;
    }
}