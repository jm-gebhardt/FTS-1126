using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.ConveyorTelegramApi.Services;

namespace Gebhardt.Acc.ConveyorTelegramApi.Telegrams;

/// <summary>
/// The AgvWeStation telegram
/// </summary>
public class AgvWeStation: ConveyorTelegram
{
    /// <summary>
    /// The station to pick or drop from.
    /// </summary>
    public string Station { get; private set; }

    /// <summary>
    /// The position of the hu to pick next.
    /// </summary>
    public string Pick { get; private set; }

    /// <summary>
    /// The position of the hu to drop.
    /// </summary>
    public string Drop { get; private set; }

    /// <summary>
    /// The handing unit.
    /// </summary>
    public string Hu { get; private set; } 

    /// <inheritdoc cref="AgvWeStation"/>>
    public AgvWeStation(string station, string? drop, string? pick, string? hu, DateTime time)
    {
        Seq = 0;
        Station = station;
        Pick = pick ?? "0000";
        Drop = drop ?? "0000";
        Hu = hu ?? string.Empty;
        Func = nameof(AgvWeStation);
        Time = time;
        Send = Fts;
        Rec = Conveyor;
    }

    /// <inheritdoc cref="AgvWeStation"/>>
    public AgvWeStation()
    {
        Seq = 0;
        Station = string.Empty;
        Hu = string.Empty;
        Pick =  "0000";
        Drop =  "0000";
        Func = nameof(AgvWeStation);
        Time = DateTime.UtcNow;
        Send = Fts;
        Rec = Conveyor;
    }

    /// <inheritdoc cref="TelegramBase"/>>
    public override bool Load(string telegram)
    {
        base.Load(telegram);
        if (Func != nameof(AgvWeStation))
        {
            return false;
        }

        Dictionary<string, string> fields = TelegramParser.Parse(telegram);

        try
        {
            Station = fields[nameof(Station)][..4].TrimEnd('.');
            Pick = fields[nameof(Pick)][..4].TrimEnd('.');
            Drop = fields[nameof(Drop)][..4].TrimEnd('.');
            Hu = fields[nameof(Hu).ToUpper()].TrimEnd('.');
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
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(SeqWidth, '0')}/{nameof(Send)}:{Send.PadRight(8, '.')}/{nameof(Rec)}:{Rec.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/{nameof(Station)}:{Station.PadRight(4, '.')}/{nameof(Pick)}:{Pick.PadRight(4, '.')}/{nameof(Drop)}:{Drop.PadRight(4, '.')}/{nameof(Hu).ToUpper()}:{Hu.PadRight(20, '.')}/{EndSymbol}";
    }
}