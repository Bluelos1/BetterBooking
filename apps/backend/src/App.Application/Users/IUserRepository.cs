using App.Domain.Users;

namespace App.Application.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByExternalIdentityAsync(string provider, string subject, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
