namespace App.Application.Common;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
