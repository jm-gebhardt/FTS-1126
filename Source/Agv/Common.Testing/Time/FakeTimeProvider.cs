using Gebhardt.StoreWare.Agv.Common.Time;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Time;

public class FakeTimeProvider : ITimeProvider
{
    private DateTime _now;

    public DateTime Now() => _now;

    public FakeTimeProvider()
    {
    }

    public FakeTimeProvider(DateTime now)
    {
        _now = now;
    }

    public void SetNow(DateTime now)
    {
        _now = now;
    }
}