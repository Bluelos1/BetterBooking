using App.Application.Audit;
using App.Application.Common;

namespace App.Application.Listings;

public sealed class ArchiveListingHandler
{
    private readonly IListingRepository _listingRepository;
    private readonly IApplicationUnitOfWork _unitOfWork;
    private readonly IAuditLog _auditLog;

    public ArchiveListingHandler(
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

        listing.Archive();

        await _auditLog.WriteAsync(new AuditLogEntry(
            AuditEventTypes.ListingArchived,
            command.OwnerUserId,
            AuditSubjectTypes.Listing,
            listing.Id), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdateListingStatusResult.Updated(listing.Id, "Archived");
    }
}
