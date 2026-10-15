namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

public interface ISimulationClock
{
    DateTimeOffset Now { get; }
    double Speedup { get; set; }
    void Advance(TimeSpan dt);
}

public sealed class SimulationClock(DateTimeOffset start, double speedup) : ISimulationClock
{
    private long _ticks = start.UtcTicks;
    private long _speedupBits = BitConverter.DoubleToInt64Bits(speedup);

    public DateTimeOffset Now => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);

    public double Speedup
    {
        get => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _speedupBits));
        set => Interlocked.Exchange(ref _speedupBits, BitConverter.DoubleToInt64Bits(Math.Max(0, value)));
    }

    public void Advance(TimeSpan dt) => Interlocked.Add(ref _ticks, dt.Ticks);
}
