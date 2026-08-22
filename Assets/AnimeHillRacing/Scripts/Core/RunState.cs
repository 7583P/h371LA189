namespace AnimeHillRacing.Core
{
    public enum RunState
    {
        Ready,
        Running,
        CrashPending,
        OutOfFuelPending,
        OutOfFuelFinal,
        Results
    }

    public enum RunEndReason
    {
        None,
        Crashed,
        DriverDown,
        OutOfFuel,
        DriverDownAndOutOfFuel,
        Finished
    }
}
