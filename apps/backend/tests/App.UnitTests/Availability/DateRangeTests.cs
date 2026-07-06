using App.Domain.Availability;

namespace App.UnitTests.Availability;

public sealed class DateRangeTests
{
    [Fact]
    public void Create_WhenEndDateIsAfterStartDate_ReturnsRange()
    {
        var range = DateRange.Create(new DateOnly(2027, 1, 10), new DateOnly(2027, 1, 13));

        Assert.Equal(3, range.Nights);
    }

    [Fact]
    public void Create_WhenEndDateEqualsStartDate_Throws()
    {
        var date = new DateOnly(2027, 1, 10);

        Assert.Throws<ArgumentException>(() => DateRange.Create(date, date));
    }

    [Theory]
    [InlineData(2027, 1, 10, 2027, 1, 12, true)]
    [InlineData(2027, 1, 12, 2027, 1, 14, false)]
    [InlineData(2027, 1, 8, 2027, 1, 10, false)]
    public void Overlaps_ReturnsExpectedResult(int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay, bool expected)
    {
        var existing = DateRange.Create(new DateOnly(2027, 1, 10), new DateOnly(2027, 1, 12));
        var candidate = DateRange.Create(new DateOnly(startYear, startMonth, startDay), new DateOnly(endYear, endMonth, endDay));

        Assert.Equal(expected, existing.Overlaps(candidate));
    }
}
