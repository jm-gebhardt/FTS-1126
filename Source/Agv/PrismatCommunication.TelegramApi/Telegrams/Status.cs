using Gebhardt.Acc.Common.TelegramCommunication.Telegram;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;

/// <summary>
/// The Status telegram.
/// </summary>
public class Status : PrismatTelegram
{
    /// <summary>
    /// The position;
    /// </summary>
    public string Pos => "FTS001";

    /// <summary>
    /// The system state.
    /// </summary>
    public SystemState State { get; private set; }

    /// <summary>
    /// The error code.
    /// </summary>
    public int Err { get; private set; }

    /// <summary>
    /// The error information.
    /// </summary>
    public string Info { get; private set; } = null!;

    /// <inheritdoc cref="Status"/>>
    public Status(SystemState state, DateTime time, int errorCode = 0, string info = "")
    {
        Seq = 0;
        Length = 216;
        Send = Fts;
        Rec = Prismat;
        Func = nameof(Status);
        State = state;
        Time = time;
        Err = errorCode;
        Info = info;
    }

    /// <inheritdoc cref="Status"/>>
    public Status()
    {
        Seq = 0;
        Length = 216;
        Send = Fts;
        Rec = Prismat;
    }

    /// <inheritdoc cref="TelegramBase"/>>
    public override bool Load(string telegram)
    {
        base.Load(telegram);
        if (Func != nameof(Status))
        {
            return false;
        }

        Dictionary<string, string> fields = TelegramParser.Parse(telegram);
        try
        {
            if(!Enum.TryParse(fields["State"].TrimEnd('.'), out SystemState result))
            {
                throw new FormatException("State could not be parsed");
            }
            State = result;
            Info = fields["Info"].TrimEnd('.');
            Err = int.Parse(fields["ERR"]);
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
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(SeqWidth, '0')}/{nameof(Send)}:{Send.PadRight(8, '.')}/{nameof(Rec)}:{Rec.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/{nameof(Pos)}:{Pos.PadRight(10, '.')}/{nameof(State)}:{State.ToString().PadRight(4, '.')}/{nameof(Err).ToUpper()}:{Err.ToString().PadLeft(4, '0')}/{nameof(Info)}:{Info.PadRight(50, '.')}/{EndSymbol}";
    }
}

/// <summary>
/// The states the system can be in.
/// </summary>
public enum SystemState
{
    /// <summary>
    /// Automatic mode
    /// </summary>
    // ReSharper disable once InconsistentNaming
    AUTO,

    /// <summary>
    /// Off, not operational.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    AUS
}