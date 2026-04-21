using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.ConveyorTelegramApi.Services;

namespace Gebhardt.Acc.ConveyorTelegramApi.Telegrams;

/// <summary>
/// The PosStateReq telegram, that allows to request a stations jam state.
/// </summary>
public class PosStateReq : ConveyorTelegram
{
    /// <summary>
    /// The position for which the state is requested.
    /// </summary>
    public string Pos { get; private set; } = null!;

    /// <inheritdoc cref="PosStateReq"/>>
    public PosStateReq(string pos, DateTime time)
    {
        Length = 216;
        Rec = Conveyor;
        Send = Fts;
        Func = nameof(PosStateReq);
        Time = time;
        Pos = pos;
    }

    /// <inheritdoc cref="PosStateReq"/>>
    public PosStateReq()
    {
        Func = nameof(PosStateReq);
        Length = 216;
        Rec = Conveyor;
        Send = Fts;
    }

    /// <inheritdoc cref="TelegramBase"/>>
    public override bool Load(string telegram)
    {
        base.Load(telegram);
        if (Func != nameof(PosStateReq))
        {
            return false;
        }
        Dictionary<string, string> fields = TelegramParser.Parse(telegram);
        try
        {
            Pos = fields["Pos"].TrimEnd('.');
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Convert to string.
    /// </summary>
    public override string ToString()
    {
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(SeqWidth, '0')}/{nameof(Send)}:{Send.PadRight(8, '.')}/{nameof(Rec)}:{Rec.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/{nameof(Pos)}:{Pos.PadRight(6, '.')}/{EndSymbol}";
    }
}