using App.Application.Common;

namespace App.Infrastructure.Persistence;

public sealed class EfApplicationUnitOfWork : IApplicationUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;

    public EfApplicationUnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var result = await operation(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch (Exception exception) when (PostgreSqlExceptionTranslator.IsActiveReservationOverlap(exception))
        {
            throw PostgreSqlExceptionTranslator.ToReservationConflict(exception);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}
