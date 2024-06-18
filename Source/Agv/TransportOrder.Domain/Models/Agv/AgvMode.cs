using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

/// <summary>
/// The mode of the agv.
/// </summary>
public record AgvMode
{
    /// <summary>
    /// The mode type.
    /// </summary>
    public ModeType Type { get; private set; }

    /// <summary>
    /// The agv correlation id.
    /// </summary>
    public Guid AgvCorrelationId { get; [UsedImplicitly]init; }

    /// <summary>
    /// The optional reason for the mode.
    /// </summary>
    public string? Reason { get;  private set; }

    /// <summary>
    /// The optional name of the one who set the mode.
    /// </summary>
    public string? Setter { get;  private set; }

    /// <inheritdoc cref="AgvMode"/>>
    public AgvMode(ModeType type, Guid agvCorrelationId, string? reason=null, string? setter=null)
    {
        Type = type;
        AgvCorrelationId = agvCorrelationId;
        Reason = reason;
        Setter = setter;
    }

    /// <summary>
    /// Switch to a new agv mode.
    /// </summary>
    /// <param name="newType">The new mode type.</param>
    /// <param name="reason">The optional reason for switching.</param>
    /// <param name="modeModeSetter">The optional setter of the new mode.</param>
    public void Switch(ModeType newType, string? reason = null, string? modeModeSetter = null)
    {
        Type = newType;
        Reason = reason;
        Setter = modeModeSetter;
    }
}

/// <summary>
/// The mode type.
/// </summary>
public enum ModeType
{
    /// <summary/>
    Normal,

    /// <summary/>
    Locked,
    
    /// <summary/>
    Maintenance,
    
    /// <summary/>
    UserMode
}