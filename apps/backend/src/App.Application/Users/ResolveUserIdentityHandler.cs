using App.Application.Audit;
using App.Application.Common;
using App.Domain.Users;

namespace App.Application.Users;

public sealed class ResolveUserIdentityHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IApplicationUnitOfWork _unitOfWork;
    private readonly IAuditLog _auditLog;
    private readonly ISystemClock _clock;

    public ResolveUserIdentityHandler(
        IUserRepository userRepository,
        IApplicationUnitOfWork unitOfWork,
        IAuditLog auditLog,
        ISystemClock clock)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
        _clock = clock;
    }

    public async Task<Guid> HandleAsync(ExternalUserIdentity identity, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByExternalIdentityAsync(
            identity.Provider,
            identity.Subject,
            cancellationToken);

        if (existingUser is not null)
        {
            existingUser.UpdateProfileHints(identity.Email, identity.DisplayName, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return existingUser.Id;
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var user = User.Create(
                Guid.NewGuid(),
                identity.Provider,
                identity.Subject,
                identity.Email,
                identity.DisplayName,
                _clock.UtcNow);

            await _userRepository.AddAsync(user, transactionCancellationToken);
            await _auditLog.WriteAsync(new AuditLogEntry(
                AuditEventTypes.UserMapped,
                user.Id,
                AuditSubjectTypes.User,
                user.Id), transactionCancellationToken);

            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);

            return user.Id;
        }, cancellationToken);
    }
}
