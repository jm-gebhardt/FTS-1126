using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApiClient.Services;

/// <summary>
/// Provides telegrams for sending
/// </summary>
public class TelegramProvider(ILogger<TelegramProvider> logger, PrismatTelegramProcessor telegramProcessor) : BackgroundService
{
    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(10, stoppingToken);

        const string posStateRequest = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:PosStateReq........./Pos:FTS001/$$:";
        logger.LogInformation("Providing test PosStateReq {Telegram}", posStateRequest);
        var telegram = new PosStateReq();
        telegram.Load(posStateRequest);
        telegramProcessor.EnqueueTelegram(telegram);

        await Task.Delay(30, stoppingToken);
        const string testSourceErrorTelegram = "/Seq:002/Send:EWM...../Rec:FTS01.../Time:2023:11:02:08:12:09:122/Func:Tord................/HU:5010000010........../Src:9920....../Dest:2110....../HuRot:1/OID:0000000000../SortID:00/OfO:01/$$:";
        logger.LogInformation("Providing test tord with source error {Telegram}", testSourceErrorTelegram);
        var tordWithSourceError = new Tord();
        tordWithSourceError.Load(testSourceErrorTelegram);
        telegramProcessor.EnqueueTelegram(tordWithSourceError);

        await Task.Delay(5, stoppingToken);

        const string testDestErrorTelegram = "/Seq:003/Send:EWM...../Rec:FTS01.../Time:2022:11:02:08:12:09:122/Func:Tord................/HU:5010000010........../Src:1020....../Dest:9999....../HuRot:1/OID:0000000000../SortID:00/OfO:01/$$:";
        logger.LogInformation("Providing test tord with source error {Telegram}", testDestErrorTelegram);
        var tordWithDestError = new Tord();
        tordWithDestError.Load(testDestErrorTelegram);
        telegramProcessor.EnqueueTelegram(tordWithDestError);

        await Task.Delay(10, stoppingToken);
        const string testTelegram = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:Tord................/HU:5010000010........../Src:1020....../Dest:2110....../HuRot:1/OID:0000000000../SortID:00/OfO:01/$$:";
        logger.LogInformation("Providing test tord {Telegram}", testTelegram);
        var tord = new Tord();
        tord.Load(testTelegram);
        telegramProcessor.EnqueueTelegram(tord);

        await Task.Delay(5, stoppingToken);

        const string testTelegramForDeletion = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2025:11:02:08:12:09:122/Func:Tord................/HU:5010000019........../Src:1040....../Dest:9070....../HuRot:1/OID:0000000000../SortID:00/OfO:01/$$:";
        logger.LogInformation("Providing test tord that is to be deleted {Telegram}", testTelegramForDeletion);
        var tordForDelete = new Tord();
        tordForDelete.Load(testTelegramForDeletion);
        telegramProcessor.EnqueueTelegram(tordForDelete);

        await Task.Delay(5, stoppingToken);
        const string testDeleteTelegram = "/Seq:004/Send:EWM...../Rec:FTS01.../Time:2025:11:02:08:12:19:122/Func:TordDelete........../HU:5010000019........../$$:";
        logger.LogInformation("Providing test tord delete {Telegram}", testDeleteTelegram);
        var tordDelete = new TordDelete();
        tordDelete.Load(testDeleteTelegram);
        telegramProcessor.EnqueueTelegram(tordDelete);
    }
}