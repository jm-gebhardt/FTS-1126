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
        const string testTelegram = "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:Tord................/HU:5010000010........../Src:1020....../Dest:2110....../HuRot:1/OID:0000000000../SortID:00/OfO:01/$$:";
        logger.LogInformation("Providing test tord {Telegram}", testTelegram);
        var tord = new Tord();
        tord.Load(testTelegram);
        telegramProcessor.EnqueueTelegram(tord);

        await Task.Delay(10, stoppingToken);
        const string testDeleteTelegram = "/Seq:002/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:19:122/Func:TordDelete........../HU:1234567890........../$$:";
        logger.LogInformation("Providing test tord delete {Telegram}", testDeleteTelegram);
        var tordDelete = new TordDelete();
        tordDelete.Load(testDeleteTelegram);
        telegramProcessor.EnqueueTelegram(tordDelete);
    }
}