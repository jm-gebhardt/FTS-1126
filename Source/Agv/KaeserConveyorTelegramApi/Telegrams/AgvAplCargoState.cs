using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;

namespace Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;

/// <summary>
/// The AgvAplCargoState telegram
/// </summary>
public class AgvAplCargoState : ConveyorTelegram
{
    /// <summary>
    /// True, if the agv comes with cargo.
    /// </summary>
    public bool Cargo { get; private set; }

    /// <inheritdoc cref="AgvAplCargoState"/>>
    public AgvAplCargoState(bool cargo, DateTime time)
    {
        Cargo = cargo;
        Func = nameof(AgvAplCargoState);
        Time = time;
        Send = Fts;
        Rec = Conveyor;
    }

    /// <inheritdoc cref="TelegramBase"/>>
    public override bool Load(string telegram)
    {
        base.Load(telegram);
        if (Func != nameof(AgvAplCargoState))
        {
            return false;
        }

        Dictionary<string, string> fields = TelegramParser.Parse(telegram);

        try
        {
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
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(3, '0')}/{nameof(Send)}:{Send.PadRight(8, '.')}/{nameof(Rec)}:{Rec.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/{nameof(Cargo)}:{(Cargo ? "1" : "0")}/{EndSymbol}";
    }
}