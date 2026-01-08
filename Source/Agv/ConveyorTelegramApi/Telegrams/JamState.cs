using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.ConveyorTelegramApi.Services;

namespace Gebhardt.Acc.ConveyorTelegramApi.Telegrams;

/// <summary>
/// The JamStateTelegram, that provides information on pallet jams at a station.
/// </summary>
public class JamState : ConveyorTelegram
{
    /// <summary>
    /// The position for which the state is sent.
    /// </summary>
    public string Pos { get; private set; } = null!;

    /// <summary>
    /// The state of the station.
    /// </summary>
    public int State { get; private set; }

    /// <inheritdoc cref="JamState"/>>
    public JamState()
    {
        Func = nameof(JamState);
        Length = 216;
        Rec = Conveyor;
        Send = Fts;
    }

    /// <inheritdoc cref="JamState"/>>
    public JamState(string pos, int state)
    {
        Func = nameof(JamState);
        Length = 216;
        Rec = Conveyor;
        Send = Fts;
        Pos = pos;
        State = state;
    }

    /// <inheritdoc cref="TelegramBase"/>>
    public override bool Load(string telegram)
    {
        base.Load(telegram);
        if (Func != nameof(JamState))
        {
            return false;
        }
        Dictionary<string, string> fields = TelegramParser.Parse(telegram);
        try
        {
            Pos = fields["Pos"].TrimEnd('.');
            State = int.Parse(fields["State"]);
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
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(3, '0')}/{nameof(Send)}:{Send.PadRight(8, '.')}/{nameof(Rec)}:{Rec.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/{nameof(Pos)}:{Pos.PadRight(6, '.')}/{nameof(State)}:{State.ToString().PadRight(1, '.')}/{EndSymbol}";
    }
}