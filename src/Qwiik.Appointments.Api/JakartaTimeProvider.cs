namespace Qwiik.Appointments.Api;

public sealed class JakartaTimeProvider : TimeProvider
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
    public override TimeZoneInfo LocalTimeZone => Zone;
}
