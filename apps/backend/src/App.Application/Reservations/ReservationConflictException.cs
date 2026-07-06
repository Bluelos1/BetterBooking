namespace App.Application.Reservations;

public sealed class ReservationConflictException : Exception
{
    public ReservationConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
