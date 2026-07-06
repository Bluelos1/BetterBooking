namespace App.Domain.Availability;

public readonly record struct DateRange(DateOnly StartDate, DateOnly EndDate)
{
    public int Nights => EndDate.DayNumber - StartDate.DayNumber;

    public static DateRange Create(DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
        {
            throw new ArgumentException("End date must be after start date.", nameof(endDate));
        }

        return new DateRange(startDate, endDate);
    }

    public bool Overlaps(DateRange other) => StartDate < other.EndDate && other.StartDate < EndDate;
}
