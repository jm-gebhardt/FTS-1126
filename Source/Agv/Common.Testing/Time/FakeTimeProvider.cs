using Gebhardt.StoreWare.Agv.Common.Time;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Time;

public class FakeTimeProvider : ITimeProvider
{
    private DateTime _now;

    public DateTime Now()
    {
        return _now;
    }

    public void SetNow(DateTime now)
    {
        _now = now;
    }
}