using Gebhardt.Acc.Common.TelegramCommunication.Telegram;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;

/// <summary>
/// The arrival telegram
/// </summary>
public class Arrival: PrismatTelegram
{
    /// <summary>
    /// The handing unit.
    /// </summary>
    public string Hu { get; private set; }

    /// <summary>
    /// The position of arrival.
    /// </summary>
    public string Pos { get; private set; }

    /// <inheritdoc cref="Arrival"/>>
    public Arrival(DateTime time, string pos, string hu)
    {
        Seq = 0;
        Hu = hu;
        Time = time;
        Pos = pos;
        Send = Fts;
        Rec = Prismat;
        Func = nameof(Arrival);
    }

    /// <inheritdoc cref="TelegramBase"/>>
    public override bool Load(string telegram)
    {
        base.Load(telegram);
        if (Func != nameof(Arrival))
        {
            return false;
        }
        Dictionary<string, string> fields = TelegramParser.Parse(telegram);
        try
        {
            Hu = fields["HU"].TrimEnd('.');
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
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(SeqWidth, '0')}/{nameof(Send)}:{Send.PadRight(8, '.')}/{nameof(Rec)}:{Rec.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/HU:{Hu.PadRight(20, '.')}/{nameof(Pos)}:{Pos.PadRight(10, '.')}/{EndSymbol}";
    }
}