namespace Gebhardt.StoreWare.Agv.Common.Services;

/// <summary>
/// Static class as replacement for DateTime, to being able to unit test
/// </summary>
public static class TimeProvider
{
    private static Func<DateTime> _dateTimeNowFunc = () => DateTime.UtcNow;

    /// <summary>
    /// DateTime Now
    /// </summary>
    public static DateTime Now => _dateTimeNowFunc();

    /// <summary>
    /// Overwrites the function to retrieve the DateTime
    /// </summary>
    /// <param name="dateTimeNowFunc"></param>
    public static void Set(Func<DateTime> dateTimeNowFunc)
    {
        _dateTimeNowFunc = dateTimeNowFunc;
    }
}