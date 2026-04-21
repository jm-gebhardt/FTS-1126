using Gebhardt.Acc.Common.TelegramCommunication.Telegram;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;

/// <summary>
/// The pick telegram
/// </summary>
public class Pick : PrismatTelegram
{
    /// <summary>
    /// The handing unit.
    /// </summary>
    public string Hu { get; private set; }

    /// <summary>
    /// The position of the pick.
    /// </summary>
    public string Pos { get; private set; }

    /// <inheritdoc cref="Pick"/>>
    public Pick(string hu, string pos, DateTime time)
    {
        Seq = 0;
        Send = Fts;
        Rec = Prismat;
        Func = nameof(Pick);
        Hu = hu;
        Pos = pos;
        Time = time;
    }

    /// <inheritdoc cref="TelegramBase"/>>
    public override bool Load(string telegram)
    {
        base.Load(telegram);
        if (Func != nameof(Pick))
        {
            return false;
        }

        Dictionary<string, string> fields = TelegramParser.Parse(telegram);
        try
        {
            Hu = fields["HU"].TrimEnd('.');
            Pos = fields["Pos"].TrimEnd('.');
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
        catch (FormatException ex)
        {
            Console.WriteLine(ex.Message);
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