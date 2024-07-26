using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Services;

namespace Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;

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
    /// True, if the agv comes with cargo.
    /// </summary>
    public bool Cargo { get; private set; }

    /// <summary>
    /// The position of the hu to pick next.
    /// </summary>
    public string Pick { get; private set; }

    /// <inheritdoc cref="AgvWeStation"/>>
    public AgvWeStation(string station, bool cargo, string? pick, DateTime time)
    {
        Station = station;
        Cargo = cargo;
        Pick = pick ?? "0000";
        Func = nameof(AgvWeStation);
        Time = time;
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
            Cargo = fields[nameof(Cargo)] == "1";
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
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(3, '0')}/{nameof(Send)}:{Send.PadRight(8, '.')}/{nameof(Rec)}:{Rec.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/{nameof(Station)}:{Station.PadRight(4, '.')}/{nameof(Cargo)}:{(Cargo?"1":"0")}/{nameof(Pick)}:{Pick.PadRight(4, '.')}/{EndSymbol}";
    }
}