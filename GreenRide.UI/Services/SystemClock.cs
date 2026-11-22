namespace GreenRide.UI.Services;

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}
