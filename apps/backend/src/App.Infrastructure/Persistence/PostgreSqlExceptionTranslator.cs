using App.Application.Reservations;
using Npgsql;

namespace App.Infrastructure.Persistence;

internal static class PostgreSqlExceptionTranslator
{
    private const string ActiveReservationOverlapConstraintName = "ex_reservations_no_active_overlap";

    public static bool IsActiveReservationOverlap(Exception exception)
    {
        var postgresException = FindPostgresException(exception);

        if (postgresException is null)
        {
            return false;
        }

        if (postgresException.SqlState == PostgresErrorCodes.ExclusionViolation)
        {
            return string.Equals(
                postgresException.ConstraintName,
                ActiveReservationOverlapConstraintName,
                StringComparison.Ordinal);
        }

        return postgresException.SqlState is PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure;
    }

    public static ReservationConflictException ToReservationConflict(Exception exception)
    {
        return new ReservationConflictException("The listing is not available for the selected dates.", exception);
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        var current = exception;

        while (current is not null)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }

            current = current.InnerException;
        }

        return null;
    }
}
