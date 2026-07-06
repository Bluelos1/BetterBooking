using App.Application.Audit;
using App.Application.Common;
using App.Application.Listings;
using App.Domain.Availability;
using App.Domain.Reservations;

namespace App.Application.Reservations;

public sealed class CreateReservationHandler
{
    private readonly IListingRepository _listingRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IApplicationUnitOfWork _unitOfWork;
    private readonly IAuditLog _auditLog;
    private readonly ISystemClock _clock;

    public CreateReservationHandler(
        IListingRepository listingRepository,
        IReservationRepository reservationRepository,
        IApplicationUnitOfWork unitOfWork,
        IAuditLog auditLog,
        ISystemClock clock)
    {
        _listingRepository = listingRepository;
        _reservationRepository = reservationRepository;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
        _clock = clock;
    }

    public async Task<CreateReservationResult> HandleAsync(CreateReservationCommand command, CancellationToken cancellationToken)
    {
        if (command.ReservationId == Guid.Empty)
        {
            return CreateReservationResult.ValidationFailed("Reservation id is required.");
        }

        if (command.ListingId == Guid.Empty)
        {
            return CreateReservationResult.ValidationFailed("Listing id is required.");
        }

        if (command.GuestUserId == Guid.Empty)
        {
            return CreateReservationResult.ValidationFailed("Guest user id is required.");
        }

        DateRange period;

        try
        {
            period = DateRange.Create(command.StartDate, command.EndDate);
        }
        catch (ArgumentException exception)
        {
            return CreateReservationResult.ValidationFailed(exception.Message);
        }

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
            {
                var isPublished = await _listingRepository.IsPublishedAsync(command.ListingId, transactionCancellationToken);

                if (!isPublished)
                {
                    await _auditLog.WriteAsync(new AuditLogEntry(
                        AuditEventTypes.ReservationRejected,
                        command.GuestUserId,
                        AuditSubjectTypes.Listing,
                        command.ListingId), transactionCancellationToken);

                    await _unitOfWork.SaveChangesAsync(transactionCancellationToken);

                    return CreateReservationResult.ListingUnavailable();
                }

                var hasOverlap = await _reservationRepository.HasActiveOverlapAsync(
                    command.ListingId,
                    period,
                    transactionCancellationToken);

                if (hasOverlap)
                {
                    await _auditLog.WriteAsync(new AuditLogEntry(
                        AuditEventTypes.ReservationRejected,
                        command.GuestUserId,
                        AuditSubjectTypes.Listing,
                        command.ListingId), transactionCancellationToken);

                    await _unitOfWork.SaveChangesAsync(transactionCancellationToken);

                    return CreateReservationResult.ListingUnavailable();
                }

                var reservation = Reservation.CreatePending(
                    command.ReservationId,
                    command.ListingId,
                    command.GuestUserId,
                    period,
                    _clock.UtcNow);

                await _reservationRepository.AddAsync(reservation, transactionCancellationToken);
                await _auditLog.WriteAsync(new AuditLogEntry(
                    AuditEventTypes.ReservationCreated,
                    command.GuestUserId,
                    AuditSubjectTypes.Reservation,
                    reservation.Id), transactionCancellationToken);

                await _unitOfWork.SaveChangesAsync(transactionCancellationToken);

                return CreateReservationResult.Created(reservation.Id);
            }, cancellationToken);
        }
        catch (ReservationConflictException)
        {
            return CreateReservationResult.ListingUnavailable();
        }
    }
}
