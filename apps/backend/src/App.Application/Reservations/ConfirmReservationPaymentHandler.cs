using App.Application.Audit;
using App.Application.Common;

namespace App.Application.Reservations;

public sealed class ConfirmReservationPaymentHandler
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IApplicationUnitOfWork _unitOfWork;
    private readonly IAuditLog _auditLog;
    private readonly ISystemClock _clock;

    public ConfirmReservationPaymentHandler(
        IReservationRepository reservationRepository,
        IApplicationUnitOfWork unitOfWork,
        IAuditLog auditLog,
        ISystemClock clock)
    {
        _reservationRepository = reservationRepository;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
        _clock = clock;
    }

    public async Task<ConfirmReservationPaymentResult> HandleAsync(
        ConfirmReservationPaymentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ReservationId == Guid.Empty)
        {
            return ConfirmReservationPaymentResult.ValidationFailed("Reservation id is required.");
        }

        if (command.GuestUserId == Guid.Empty)
        {
            return ConfirmReservationPaymentResult.ValidationFailed("Guest user id is required.");
        }

        var reservation = await _reservationRepository.GetByIdAsync(command.ReservationId, cancellationToken);

        if (reservation is null)
        {
            return ConfirmReservationPaymentResult.NotFound();
        }

        if (reservation.GuestUserId != command.GuestUserId)
        {
            return ConfirmReservationPaymentResult.Forbidden();
        }

        try
        {
            reservation.ConfirmPayment(_clock.UtcNow);

            await _auditLog.WriteAsync(new AuditLogEntry(
                AuditEventTypes.PaymentConfirmed,
                command.GuestUserId,
                AuditSubjectTypes.Reservation,
                reservation.Id), cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ConfirmReservationPaymentResult.Confirmed(
                reservation.Id,
                reservation.Status.ToString(),
                reservation.PaymentStatus.ToString());
        }
        catch (InvalidOperationException exception)
        {
            return ConfirmReservationPaymentResult.InvalidState(exception.Message);
        }
    }
}
