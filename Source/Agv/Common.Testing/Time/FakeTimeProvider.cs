namespace Gebhardt.Acc.Common.Testing.Time;

public class FakeTimeProvider : TimeProvider
{
    private bool _useRealNow;
    private DateTimeOffset _now;

    public FakeTimeProvider(bool useRealNow = true)
    {
        _useRealNow = useRealNow;
    }

    public FakeTimeProvider(DateTimeOffset now)
    {
        _now = now;
        _useRealNow = false;
    }

    public void SetNow(DateTimeOffset now)
    {
        _now = now;
        _useRealNow = false;
    }

    public override DateTimeOffset GetUtcNow() => _useRealNow ? DateTimeOffset.UtcNow : _now;
}