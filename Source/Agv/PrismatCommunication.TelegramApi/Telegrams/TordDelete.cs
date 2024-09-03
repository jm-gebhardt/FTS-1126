using Gebhardt.Acc.Common.TelegramCommunication.Telegram;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;

/// <summary>
/// The TordDelete telegram (transport order)
/// </summary>
public class TordDelete: PrismatTelegram
{
    /// <summary>
    /// The handing unit.
    /// </summary>
    public string Hu { get; private set; } = null!;

    /// <inheritdoc cref="TordDelete"/>>
    public TordDelete()
    {
    }
    
    /// <inheritdoc cref="TelegramBase"/>>
    public override bool Load(string telegram)
    {
        base.Load(telegram);
        if (Func != nameof(TordDelete))
        {
            return false;
        }
        Dictionary<string, string> fields = TelegramParser.Parse(telegram);
        try
        {
            Hu = fields["HU"].TrimEnd('.');
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
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(3, '0')}/{nameof(Send)}:{Send.PadRight(8, '.')}/{nameof(Rec)}:{Rec.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/HU:{Hu.PadRight(20, '.')}/{EndSymbol}";
    }
}