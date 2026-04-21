using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.ConveyorTelegramApi.Services;
using JetBrains.Annotations;

namespace Gebhardt.Acc.ConveyorTelegramApi.Telegrams;

/// <summary>
/// The Tord telegram.
/// </summary>
[PublicAPI]
public class Tord: ConveyorTelegram
{
    /// <summary>
    /// The handing unit.
    /// </summary>
    public string Hu { get; private set; } = null!;

    /// <summary>
    /// The source.
    /// </summary>
    public string Src { get; private set; } = null!;

    /// <summary>
    /// The destination.
    /// </summary>
    public string Dest { get; private set; } = null!;
   
    /// <inheritdoc cref="Tord"/>>
    public Tord()
    {
        Length = 216;
    }

    /// <inheritdoc cref="Tord"/>>
    public Tord(string src, string? dest, string? hu, DateTime time)
    {
        Seq = 0;
        Src = src;
        Dest = dest ?? "0000";
        Hu = hu ?? string.Empty;
        Func = nameof(Tord);
        Time = time;
        Send = Fts;
        Rec = Conveyor;
    }

    /// <inheritdoc cref="TelegramBase"/>>
    public override bool Load(string telegram)
    {
        base.Load(telegram);
        if (Func != nameof(Tord))
        {
            return false;
        }
        Dictionary<string, string> fields = TelegramParser.Parse(telegram);
        try
        {
            Hu = fields["HU"].TrimEnd('.');
            Src = fields["Src"].TrimEnd('.');
            Dest = fields["Dest"].TrimEnd('.');
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
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(SeqWidth, '0')}/{nameof(Send)}:{Send.PadRight(8, '.')}/{nameof(Rec)}:{Rec.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/HU:{Hu.PadRight(20, '.')}/{nameof(Src)}:{Src.PadRight(10, '.')}/{nameof(Dest)}:{Dest.PadRight(10, '.')}/{EndSymbol}";
    }
}