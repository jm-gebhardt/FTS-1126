using Gebhardt.Acc.Common.TelegramCommunication.Telegram;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;

/// <summary>
/// The tord error telegram (transport order error)
/// </summary>
public class TordError : PrismatTelegram
{
    /// <summary>
    /// The handing unit.
    /// </summary>
    public string Hu { get; private set; } = null!;

    /// <summary>
    /// The error code.
    /// </summary>
    public int Err { get; private set; }

    /// <summary>
    /// The source.
    /// </summary>
    public string Src { get; private set; } = null!;

    /// <summary>
    /// The destination.
    /// </summary>
    public string Dest { get; private set; } = null!;

    /// <summary>
    /// The error information.
    /// </summary>
    public string Info { get; private set; } = null!;

    /// <inheritdoc cref="TordError"/>>
    public TordError(string hu, string source, string destination, string info, int errorCode, DateTime time)
    {
        Seq = 0;
        Length = 216;
        Send = Fts;
        Rec = Prismat;
        Func = nameof(TordError);
        Hu = hu;
        Time = time;
        Err = errorCode;
        Src = source;
        Dest = destination;
        Info = info;
    }

    /// <inheritdoc cref="TordError"/>>
    public TordError()
    {
        Length = 216;
    }

    /// <inheritdoc cref="TelegramBase"/>>
    public override bool Load(string telegram)
    {
        base.Load(telegram);
        if (Func != nameof(TordError))
        {
            return false;
        }

        Dictionary<string, string> fields = TelegramParser.Parse(telegram);
        try
        {
            Hu = fields["HU"].TrimEnd('.');
            Src = fields["Src"].TrimEnd('.');
            Dest = fields["Dest"].TrimEnd('.');
            Info = fields["Info"].TrimEnd('.');
            Err = int.Parse(fields["Err"]);
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
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(SeqWidth, '0')}/{nameof(Send)}:{Send.PadRight(8, '.')}/{nameof(Rec)}:{Rec.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/HU:{Hu.PadRight(20, '.')}/{nameof(Err)}:{Err.ToString().PadLeft(4, '0')}/{nameof(Src)}:{Src.PadRight(10, '.')}/{nameof(Dest)}:{Dest.PadRight(10, '.')}/{nameof(Info)}:{Info.PadRight(50, '.')}/{EndSymbol}";
    }
}