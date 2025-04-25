using Gebhardt.Acc.PrismatCommunication.Domain.Models.Telegram;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;

/// <summary>
/// Provides extension methods for <see cref="Telegram"/>
/// </summary>
public static class TelegramExtension
{
    /// <summary>
    /// Convert to <see cref="PrismatTelegram"/>.
    /// </summary>
    public static PrismatTelegram ToPrismatTelegram(this Telegram telegram)
    {
        switch (telegram.Function)
        {
            case nameof(TordError):
            {
                var tordError = new TordError();
                tordError.Load(telegram.Message);
                return tordError;
            }

            case nameof(Pick):
            {
                var pick = new Pick("hu", "pos", DateTime.UtcNow);
                pick.Load(telegram.Message);
                return pick;
            }

            case nameof(Arrival):
            {
                var arrival = new Arrival(DateTime.UtcNow, "hu", "pos");
                arrival.Load(telegram.Message);
                return arrival;
            }

            case nameof(Status):
            {
                var status = new Status();
                status.Load(telegram.Message);
                return status;
            }
        }
        var prismatTelegram = new PrismatTelegram();
        prismatTelegram.Load(telegram.Message);
        prismatTelegram.Load(telegram.Message);
        return prismatTelegram;
    }
}