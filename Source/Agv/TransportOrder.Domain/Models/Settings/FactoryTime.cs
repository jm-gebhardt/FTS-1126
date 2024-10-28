using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Settings;

/// <summary>
/// Provides a factory time.
/// </summary>
public class FactoryTime
{
    /// <inheritdoc cref="FactoryTime"/>
    public FactoryTime(TimeZoneInfo factoryTimeZone)
    {
        SerializedFactoryTimeZone = factoryTimeZone.Id;
        _factoryTimeZone = factoryTimeZone;
    }

    /// <inheritdoc cref="FactoryTime"/>
    [UsedImplicitly]
    public FactoryTime(string serializedFactoryTimeZone)
    {
        SerializedFactoryTimeZone = serializedFactoryTimeZone;

        TimeZoneInfo.TryFindSystemTimeZoneById(serializedFactoryTimeZone, out var readTimeZone);
        if (readTimeZone == null)
        {
            Dirty = true;
            _factoryTimeZone = TimeZoneInfo.Local;
        }
        else
        {
            _factoryTimeZone = readTimeZone;
        }
    }

    /// <summary>
    /// The identifier of the factory time.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The time zone of the factory time.
    /// </summary>
    public TimeZoneInfo FactoryTimeZone 
    {
        get => _factoryTimeZone;
        set
        {
            SerializedFactoryTimeZone = value.Id;
            _factoryTimeZone = value;
        }
    }

    private TimeZoneInfo _factoryTimeZone;

    /// <summary>
    /// Serialized version of <see cref="FactoryTimeZone"/>.
    /// </summary>
    public string SerializedFactoryTimeZone { get; private set; }

    /// <summary>
    /// Whether this <see cref="FactoryTime"/> was incorrectly read.
    /// </summary>
    public bool Dirty { get; private set; }
}