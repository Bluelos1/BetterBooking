using App.Application.Audit;
using App.Application.Common;

namespace App.Application.Reservations;

public sealed class CancelReservationHandler
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IApplicationUnitOfWork _unitOfWork;
    private readonly IAuditLog _auditLog;
    private readonly ISystemClock _clock;

    public CancelReservationHandler(
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

    public async Task<CancelReservationResult> HandleAsync(CancelReservationCommand command, CancellationToken cancellationToken)
    {
        if (command.ReservationId == Guid.Empty)
        {
            return CancelReservationResult.ValidationFailed("Reservation id is required.");
        }

        if (command.GuestUserId == Guid.Empty)
        {
            return CancelReservationResult.ValidationFailed("Guest user id is required.");
        }

        var reservation = await _reservationRepository.GetByIdAsync(command.ReservationId, cancellationToken);

        if (reservation is null)
        {
            return CancelReservationResult.NotFound();
        }

        if (reservation.GuestUserId != command.GuestUserId)
        {
            return CancelReservationResult.Forbidden();
        }

        try
        {
            reservation.Cancel(_clock.UtcNow);

            await _auditLog.WriteAsync(new AuditLogEntry(
                AuditEventTypes.ReservationCancelled,
                command.GuestUserId,
                AuditSubjectTypes.Reservation,
                reservation.Id), cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return CancelReservationResult.Cancelled(reservation.Id, reservation.PaymentStatus.ToString());
        }
        catch (InvalidOperationException exception)
        {
            return CancelReservationResult.InvalidState(exception.Message);
        }
    }
}
