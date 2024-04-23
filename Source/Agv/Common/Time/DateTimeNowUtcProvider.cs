namespace Gebhardt.Acc.Common.Time;

/// <inheritdoc />
public class DateTimeNowUtcProvider : ITimeProvider
{
    private readonly Func<DateTime> _dateTimeNowFunc = () => DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime Now()
    {
        return _dateTimeNowFunc();
    }
}