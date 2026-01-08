using System.Globalization;
using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.ConveyorTelegramApi.Services;

namespace Gebhardt.Acc.ConveyorTelegramApi.Telegrams;

/// <summary>
/// A conveyor telegram
/// </summary>
public class ConveyorTelegram: TelegramBase
{
    /// <summary>
    /// The FTS sender/receiver id for conveyor telegrams
    /// </summary>
    protected const string Fts = "FTS01";

    /// <summary>
    /// The MFR sender/receiver id for conveyor telegrams
    /// </summary>
    protected const string Conveyor = "UST";

    /// <inheritdoc/>
    public ConveyorTelegram()
    {
        EndSymbol = "$$:";
        Length = 256;
    }

    /// <inheritdoc cref="TelegramBase"/>>
    public override bool Load(string telegram)
    {
        Dictionary<string, string> fields = TelegramParser.Parse(telegram);

        try
        {
            Seq = int.Parse(fields["Seq"]);
            Send = fields["Send"].TrimEnd('.');
            Rec = fields["Rec"].TrimEnd('.');
            Time = DateTime.ParseExact(GetTime(telegram), "yyyy:MM:dd:HH:mm:ss:fff", CultureInfo.InvariantCulture);
            Func =  fields["Func"].TrimEnd('.');
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

    private string GetTime(string telegram)
    {
        const string key = $"{nameof(Time)}:";
        int startIndex = telegram.IndexOf(key, StringComparison.Ordinal) + key.Length;
        int endIndex = telegram.IndexOf('/', startIndex);
        string time = telegram.Substring(startIndex, endIndex - startIndex);
        return time;
    }

    /// <inheritdoc cref="TelegramBase"/>>
    public override string CreateAcknowledgement()
    {
        return $"/{nameof(Seq)}:{Seq.ToString().PadLeft(3, '0')}/{nameof(Send)}:{Rec.PadRight(8, '.')}/{nameof(Rec)}:{Send.PadRight(8, '.')}/{nameof(Time)}:{ConvertTime()}/{nameof(Func)}:{Func.PadRight(20, '.')}/{EndSymbol}";
    }
}