
namespace Gebhardt.Acc.ConveyorTelegramApi.Telegrams;

/// <summary>
/// The Life telegram
/// </summary>
public class Life : ConveyorTelegram
{
    /// <inheritdoc cref="Life"/>>
    public Life(DateTime now)
    {
        Time = now;
        Func = nameof(Life);
        Send = Fts;
        Rec = Conveyor;
    }

    /// <summary>
    /// Convert to string.
    /// </summary>
    public override string ToString()
    {
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(SeqWidth, '0')}/{nameof(Send)}:{Send.PadRight(8, '.')}/{nameof(Rec)}:{Rec.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/{EndSymbol}";
    }
}