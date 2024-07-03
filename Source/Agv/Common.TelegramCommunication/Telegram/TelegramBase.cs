using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.TelegramCommunication.Telegram;

/// <summary>
/// Provides a base class for telegram implementations
/// </summary>
public abstract class TelegramBase
{
    /// <summary>
    /// The length of the telegram
    /// </summary>
    [PublicAPI]
    public int Length { get; set; }

    /// <summary>
    /// The end symbol
    /// </summary>
    public string EndSymbol { get; protected init; } = "$$:";

    /// <summary>
    /// Sequence number
    /// </summary>
    public int Seq { get; set; } = -1;

    /// <summary>
    /// Sender 
    /// </summary>
    public string Send { get; set; } = null!;

    /// <summary>
    /// Receiver 
    /// </summary>
    public string Rec { get; set; } = null!;

    /// <summary>
    /// Timestamp in yyyy:MM:dd:hh:mm:ss:mss 
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// Function of the telegram
    /// </summary>
    public string Func { get; set; } = null!;

    /// <summary>
    /// Load the telegram 
    /// </summary>
    /// <param name="telegram">The telegram</param>
    /// <returns>True if loading was successful</returns>
    [PublicAPI]
    public abstract bool Load(string telegram);

    /// <summary>
    /// Creates an acknowledgement to the message
    /// </summary>
    /// <returns></returns>
    [PublicAPI]
    public virtual string CreateAcknowledgement()
    {
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(3, '0')}/{nameof(Send)}:{Rec.PadRight(4, '0')}/{nameof(Rec)}:{Send.PadRight(4, '0')}/{nameof(Time)}:{Time}/{EndSymbol}";
    }
}