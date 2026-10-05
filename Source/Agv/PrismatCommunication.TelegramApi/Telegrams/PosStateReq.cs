using Gebhardt.Acc.Common.TelegramCommunication.Telegram;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;

/// <summary>
/// The position state request (PosStateReq) telegram
/// </summary>
public class PosStateReq : PrismatTelegram
{
    /// <summary>
    /// The position for which the state is requested.
    /// </summary>
    public string Pos { get; private set; } = null!;

    /// <inheritdoc cref="PosStateReq"/>>
    public PosStateReq(string pos, DateTime time)
    {
        Length = 216;
        Rec = Fts;
        Send = Prismat;
        Func = nameof(PosStateReq);
        Time = time;
        Pos = pos;
    }

    /// <inheritdoc cref="PosStateReq"/>>
    public PosStateReq()
    {
        Func = nameof(PosStateReq);
        Length = 216;
        Rec = Fts;
        Send = Prismat;
    }

    /// <inheritdoc cref="TelegramBase"/>>
    public override bool Load(string telegram)
    {
        base.Load(telegram);
        if (Func != nameof(PosStateReq))
        {
            return false;
        }
        var fields = TelegramParser.Parse(telegram);
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