namespace Gebhardt.Acc.KaeserSidecar.Domain.Models;

/// <summary>
/// The target station.
/// </summary>
public class TargetStation
{
    private readonly object _lock = new();
    private string _last = null!;

    /// <summary>
    /// The last value of the target station.
    /// </summary>
    public string Last
    {
        get
        {
            lock (_lock)
            {
                return _last;
            }
        }
        set
        {
            lock (_lock)
            {
                _last = value;
            }
        }
    }
}