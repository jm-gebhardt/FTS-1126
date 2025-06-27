using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;

/// <summary>
/// Provides a factory for telegram processors
/// </summary>
[UsedImplicitly]
public class TelegramProcessorFactory(IServiceProvider provider, ILogger<TelegramProcessorFactory> logger)
{
    /// <summary>
    /// Get a processor for the next prismat telegram.
    /// </summary>
    /// <param name="telegram">The telegram to process</param>
    public virtual ITelegramProcessor? GetReceiveProcessor(PrismatTelegram telegram)
    {
        switch (telegram.Func)
        {
            case nameof(Tord):
            {
                logger.LogDebug("Tord processor requested");
                return provider.GetRequiredService<TordProcessor>();
            }

            case nameof(Life):
            {
                logger.LogDebug("Life processor requested");
                return provider.GetRequiredService<LifeProcessor>();
            }

            case nameof(TordDelete):
            {
                logger.LogDebug("TordDelete processor requested");
                return provider.GetRequiredService<TordDeleteProcessor>();
            }

            case nameof(PosStateReq):
            {
                logger.LogDebug("PosStateReq processor requested");
                return provider.GetRequiredService<PosStateReqProcessor>();
            }

            default:
                logger.LogWarning("No telegram processor found for telegram function {Func}", telegram.Func);
                return null;
        }
    }
}