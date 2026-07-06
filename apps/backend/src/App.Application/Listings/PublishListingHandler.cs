using App.Application.Audit;
using App.Application.Common;

namespace App.Application.Listings;

public sealed class PublishListingHandler
{
    private readonly IListingRepository _listingRepository;
    private readonly IApplicationUnitOfWork _unitOfWork;
    private readonly IAuditLog _auditLog;

    public PublishListingHandler(
        IListingRepository listingRepository,
        IApplicationUnitOfWork unitOfWork,
        IAuditLog auditLog)
    {
        _listingRepository = listingRepository;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<PublishListingResult> HandleAsync(PublishListingCommand command, CancellationToken cancellationToken)
    {
        var listing = await _listingRepository.GetByIdAsync(command.ListingId, cancellationToken);

        if (listing is null)
        {
            return PublishListingResult.NotFound();
        }

        if (!listing.IsOwnedBy(command.OwnerUserId))
        {
            return PublishListingResult.Forbidden();
        }

        try
        {
            listing.Publish();

            await _auditLog.WriteAsync(new AuditLogEntry(
                AuditEventTypes.ListingPublished,
                command.OwnerUserId,
                AuditSubjectTypes.Listing,
                listing.Id), cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return PublishListingResult.Published(listing.Id);
        }
        catch (InvalidOperationException exception)
        {
            return PublishListingResult.InvalidState(exception.Message);
        }
    }
}
