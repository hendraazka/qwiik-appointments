namespace Qwiik.Appointments.Core;

public class SlotConflictException : Exception
{
    public SlotConflictException(string message, Exception? inner = null) : base(message, inner) { }
}
