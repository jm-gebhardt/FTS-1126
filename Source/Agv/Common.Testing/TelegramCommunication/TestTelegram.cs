using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Testing.TelegramCommunication;

public static class TestTelegram
{
    public const string Tord = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:Tord................/HU:5010000010........../Src:1020....../Dest:2110....../HuRot:1/OID:0000000000../SortID:1./OfO:3./$$:";
    
    [PublicAPI]
    public const string TordWithSourceError = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:Tord................/HU:5010000010........../Src:9999....../Dest:2110....../HuRot:1/OID:0000000000../SortID:1./OfO:3./$$:";
    
    [PublicAPI]
    public const string TordWithDestinationError = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:Tord................/HU:5010000010........../Src:1020....../Dest:9999....../HuRot:1/OID:0000000000../SortID:1./OfO:3./$$:";

    [PublicAPI]
    public const string TordDelete = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:TordDelete........../HU:1234567890........../$$:";

    public const string Pick = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:Pick................/HU:1234567890........../Pos:1040....../$$:";
    public const string Arrival = "/Seq:001/Send:FTS01.../Rec:EWM...../Time:2021:11:02:08:12:09:122/Func:Arrival............./HU:1234567890........../Pos:1050....../$$:";
   
    public const string TordError = "/Seq:001/Send:FTS01.../Rec:EWM...../Time:2021:11:02:08:12:09:122/Func:TordError.........../HU:5010000010........../Err:9010/Src:1020....../Dest:9999....../Info:Ziel-Station nicht bekannt......................../$$:";
    public const string Life = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2020:12:13:20:12:09:122/Func:Life................/$$:";

    public const string Status = "/Seq:001/Send:FTS01.../Rec:EWM...../Time:2021:11:02:08:12:09:122/Func:Status............../Pos:FTS001..../State:AUTO/ERR:5000/Info:AAA.............................................../$$:";
    public const string PosStateReq = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:PosStateReq........./Pos:FTS001/$$:";
    
    [PublicAPI]
    public const string ResetCounter = "/Seq:000/Send:EWM...../Rec:FTS01.../Time:2020:12:13:20:12:09:122/Func:..................../$$:";

    public const string AgvAplCargoState = "/Seq:001/Send:FTS01.../Rec:UST...../Time:2021:11:02:08:12:09:122/Func:AgvAplCargoState..../Cargo:1/$$:";
    public const string AgvWeStation = "/Seq:001/Send:FTS01.../Rec:UST...../Time:2021:11:02:08:12:09:122/Func:AgvWeStation......../Station:WE01/Pick:1040/Drop:1060/HU:1234567890........../$$:";
}