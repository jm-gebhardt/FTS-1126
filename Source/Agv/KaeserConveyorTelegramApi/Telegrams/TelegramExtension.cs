using Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;

namespace Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;

/// <summary>
/// Provides extension methods for <see cref="Telegram"/>
/// </summary>
public static class TelegramExtension
{
    /// <summary>
    /// Convert to an implementation of <see cref="ConveyorTelegram"/>.
    /// </summary>
    /// <param name="telegram">The telegram.</param>
    public static ConveyorTelegram ToConveyorTelegram(this Telegram telegram)
    {
        switch (telegram.Function)
        {
            case nameof(AgvWeStation):
                {
                    var agvWeStationTelegram = new AgvWeStation("station", null, null, null, DateTime.UtcNow);
                    agvWeStationTelegram.Load(telegram.Message);
                    return agvWeStationTelegram;
                }

            case nameof(AgvAplCargoState):
            {
                var agvAplCargoState = new AgvAplCargoState(false, DateTime.UtcNow);
                agvAplCargoState.Load(telegram.Message);
                return agvAplCargoState;
                }
        }
        var conveyorTelegram = new ConveyorTelegram();
        conveyorTelegram.Load(telegram.Message);
        return conveyorTelegram;
    }
}