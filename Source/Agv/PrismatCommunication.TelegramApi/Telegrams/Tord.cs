using Gebhardt.Acc.Common.TelegramCommunication.Telegram;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;

/// <summary>
/// The tord telegram (transport order)
/// </summary>
public class Tord: PrismatTelegram
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

    /// <summary>
    /// The rotation of the handling unit.
    /// </summary>
    public int HuRot { get; private set; }

    /// <summary>
    /// The order id.
    /// </summary>
    public string OId { get; private set; } = null!;

    /// <summary>
    /// The sort index in the order.
    /// </summary>
    public int SortId { get; private set; }

    /// <summary>
    /// The number of handling units in the order.
    /// </summary>
    public int OfO { get; private set; }

    /// <inheritdoc cref="Tord"/>>
    public Tord()
    {
        Length = 216;
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
            OId = fields["OID"].TrimEnd('.');
            HuRot = int.Parse(fields["HuRot"].TrimEnd('.'));
            SortId = int.Parse(fields["SortID"].TrimEnd('.'));
            OfO = int.Parse(fields["OfO"].TrimEnd('.'));
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
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(3, '0')}/{nameof(Send)}:{Send.PadRight(8, '.')}/{nameof(Rec)}:{Rec.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/HU:{Hu.PadRight(20, '.')}/{nameof(Src)}:{Src.PadRight(10, '.')}/{nameof(Dest)}:{Dest.PadRight(10, '.')}/{nameof(HuRot)}:{HuRot}/OID:{OId.PadRight(12, '.')}/SortID:{SortId.ToString().PadRight(2, '.')}/OfO:{OfO.ToString().PadRight(2,'.')}/{EndSymbol}";
    }
}