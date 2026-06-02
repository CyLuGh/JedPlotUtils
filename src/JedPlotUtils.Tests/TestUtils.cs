using JedPlotUtils;

namespace TestProject1;

public class TestUtils
{
    [Theory]
    [InlineData(2023, 1, 1)]
    [InlineData(2025, 4, 1)]
    [InlineData(2000, 6, 1)]
    [InlineData(2013, 12, 24)]
    public void TestOADate(int year, int month, int day)
    {
        var date = new DateOnly(year, month, day);
        var roundAround = date.ToOADate().ToDateOnly();
        Assert.Equal(date, roundAround);
    }
}
