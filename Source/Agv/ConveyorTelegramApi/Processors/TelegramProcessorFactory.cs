using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.ConveyorTelegramApi.Processors;

/// <summary>
/// Provides a factory for telegram processors
/// </summary>
[UsedImplicitly]
public class TelegramProcessorFactory(IServiceProvider provider, ILogger<TelegramProcessorFactory> logger)
{
    /// <summary>
    /// Get a processor for the next conveyor telegram.
    /// </summary>
    /// <param name="telegram">The telegram to process</param>
    public virtual ITelegramProcessor? GetReceiveProcessor(ConveyorTelegram telegram)
    {
        switch (telegram.Func)
        {
            case nameof(JamState):
            {
                logger.LogDebug("JamState processor requested");
                return provider.GetRequiredService<JamStateProcessor>();
            }

            case nameof(Life):
            {
                logger.LogDebug("Life processor requested");
                return provider.GetRequiredService<LifeProcessor>();
            }

            default:
                logger.LogWarning("No telegram processor found");
                return null;
        }
    }
}