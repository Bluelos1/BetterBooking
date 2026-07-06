using App.Application.Audit;
using App.Application.Common;

namespace App.Application.Listings;

public sealed class UnpublishListingHandler
{
    private readonly IListingRepository _listingRepository;
    private readonly IApplicationUnitOfWork _unitOfWork;
    private readonly IAuditLog _auditLog;

    public UnpublishListingHandler(
        IListingRepository listingRepository,
        IApplicationUnitOfWork unitOfWork,
        IAuditLog auditLog)
    {
        _listingRepository = listingRepository;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<UpdateListingStatusResult> HandleAsync(UpdateListingStatusCommand command, CancellationToken cancellationToken)
    {
        var listing = await _listingRepository.GetByIdAsync(command.ListingId, cancellationToken);

        if (listing is null)
        {
            return UpdateListingStatusResult.NotFound();
        }

        if (!listing.IsOwnedBy(command.OwnerUserId))
        {
            return UpdateListingStatusResult.Forbidden();
        }

        try
        {
            listing.Unpublish();

            await _auditLog.WriteAsync(new AuditLogEntry(
                AuditEventTypes.ListingUnpublished,
                command.OwnerUserId,
                AuditSubjectTypes.Listing,
                listing.Id), cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return UpdateListingStatusResult.Updated(listing.Id, "Draft");
        }
        catch (InvalidOperationException exception)
        {
            return UpdateListingStatusResult.InvalidState(exception.Message);
        }
    }
}
