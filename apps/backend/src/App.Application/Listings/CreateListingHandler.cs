using App.Application.Audit;
using App.Application.Common;
using App.Domain.Listings;

namespace App.Application.Listings;

public sealed class CreateListingHandler
{
    private readonly IListingRepository _listingRepository;
    private readonly IApplicationUnitOfWork _unitOfWork;
    private readonly IAuditLog _auditLog;
    private readonly ISystemClock _clock;

    public CreateListingHandler(
        IListingRepository listingRepository,
        IApplicationUnitOfWork unitOfWork,
        IAuditLog auditLog,
        ISystemClock clock)
    {
        _listingRepository = listingRepository;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
        _clock = clock;
    }

    public async Task<CreateListingResult> HandleAsync(CreateListingCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
            {
                var listing = Listing.CreateDraft(
                    command.ListingId,
                    command.OwnerUserId,
                    command.Title,
                    command.Description,
                    command.Location,
                    command.NightlyPriceAmount,
                    command.MaxGuests,
                    command.BedroomCount,
                    command.BathroomCount,
                    command.HeroImageUrl,
                    command.Amenities,
                    _clock.UtcNow);

                await _listingRepository.AddAsync(listing, transactionCancellationToken);
                await _auditLog.WriteAsync(new AuditLogEntry(
                    AuditEventTypes.ListingCreated,
                    command.OwnerUserId,
                    AuditSubjectTypes.Listing,
                    listing.Id), transactionCancellationToken);

                await _unitOfWork.SaveChangesAsync(transactionCancellationToken);

                return CreateListingResult.Created(listing.Id);
            }, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return CreateListingResult.ValidationFailed(exception.Message);
        }
    }
}
