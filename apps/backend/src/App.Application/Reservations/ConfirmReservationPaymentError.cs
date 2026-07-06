namespace App.Application.Reservations;

public enum ConfirmReservationPaymentError
{
    ValidationFailed = 1,
    NotFound = 2,
    Forbidden = 3,
    InvalidState = 4
}
