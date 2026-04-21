using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.TelegramCommunication.Telegram;

/// <summary>
/// Provides a base class for telegram implementations
/// </summary>
public abstract class TelegramBase
{
    /// <summary>
    /// The total width of the sequence number when serialized.
    /// </summary>
    protected const int SeqWidth = 3;

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
    /// Convert the Time property to string.
    /// </summary>
    /// <returns></returns>
    protected string ConvertTime()
    {
        return Time.ToString("yyyy:MM:dd:HH:mm:ss:fff");
    }

    /// <summary>
    /// Returns the string representation of the telegram without the volatile Seq field.
    /// </summary>
    public string ToStringWithoutSeq()
    {
        string seqPrefix = $"/{nameof(Seq)}:{Seq.ToString().PadLeft(SeqWidth, '0')}/";
        return ToString()!.Replace(seqPrefix, "/");
    }

    /// <summary>
    /// Creates an acknowledgement to the message
    /// </summary>
    /// <returns></returns>
    [PublicAPI]
    public virtual string CreateAcknowledgement()
    {
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(SeqWidth, '0')}/{nameof(Send)}:{Rec.PadRight(8, '.')}/{nameof(Rec)}:{Send.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/{EndSymbol}";
    }
}