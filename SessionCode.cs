    /// <summary>
    /// Session status codes
    /// </summary>
    /// 
    namespace TherapyTime;
    public enum SessionCode
    {
        IC,           // Incomplete, session has not yet taken place and has not been marked as skipped for any reason.
        T,            // Session completed.
        NM,           // Needs makeup session
        MU,           // Makeup session
        R,            // session refused, Do not need to make up the session.
        A,            // Student absent, Do not need to make up the session.
        SU           // Student Unavailable, Do not need to make up session.
    }