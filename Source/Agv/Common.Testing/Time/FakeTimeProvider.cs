namespace Gebhardt.Acc.Common.Testing.Time;

public class FakeTimeProvider(bool useRealNow = true) : TimeProvider
{
    private bool _useRealNow = useRealNow;
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTimeOffset now) : this(false)
    {
        _now = now;
    }

    public void SetNow(DateTimeOffset now)
    {
        _now = now;
        _useRealNow = false;
    }

    public override DateTimeOffset GetUtcNow() => _useRealNow ? DateTimeOffset.UtcNow : _now;
}