using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Testing.TelegramCommunication;

public static class TestTelegram
{
    public const string Tord = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:Tord................/HU:1234567890........../Src:1040....../Dest:2410....../OID:1234567890../SortID:1./OfO:3./$$:";

    [PublicAPI]
    public const string TordDelete = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:Tord................/HU:1234567890........../$$:";

    [PublicAPI]
    public const string Pick = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:Tord................/HU:1234567890........../Pos:1040....../$$:";

    public const string Arrival = "/Seq:001/Send:FTS01.../Rec:EWM...../Time:2021:11:02:08:12:09:122/Func:Arrival............./HU:1234567890........../Pos:1050....../$$:";
    public const string Life = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2020:12:13:20:12:09:122/Func:Life................/$$:";
    
    [PublicAPI]
    public const string ResetCounter = "/Seq:000/Send:EWM...../Rec:FTS01.../Time:2020:12:13:20:12:09:122/Func:..................../$$:";
}