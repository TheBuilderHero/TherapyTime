

/// <summary>
    /// Represents a single therapy session
    /// </summary>
    public class TherapySession
    {
        public DateTime Date { get; set; }
        public int Minutes { get; set; }
        public SessionCode Status { get; set; } = SessionCode.IC;

        /// <summary>
        /// Notes for NM (Need Make-up) or R (Refused) sessions
        /// </summary>
        public string? Notes { get; set; } = null;

        public TherapySession() { }

        public TherapySession(DateTime date, int minutes, SessionCode status = SessionCode.T, string? notes = null)
        {
            Date = date;
            Minutes = minutes;
            Status = status;

            // Only allow notes for NM or R
            if (status == SessionCode.NM || status == SessionCode.R)
                Notes = notes;
        }
    }
