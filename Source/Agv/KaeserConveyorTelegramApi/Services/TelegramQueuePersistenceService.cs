using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Gebhardt.Acc.KaeserConveyorTelegramApi.Services;

/// <summary>
/// Persists the telegram queue.
/// </summary>
public class TelegramQueuePersistenceService : IAsyncDisposable
{
    private readonly ILogger<TelegramQueuePersistenceService> _logger;
    private readonly ObservableConcurrentQueue<ConveyorTelegram> _telegramQueue;
    private readonly ITelegramRepository _telegramRepository;
    private readonly ConcurrentQueue<Telegram> _telegramsToDelete = new();

    /// <inheritdoc cref="TelegramQueuePersistenceService"/>>
    public TelegramQueuePersistenceService(ObservableConcurrentQueue<ConveyorTelegram> telegramQueue, ITelegramRepository telegramRepository, ILogger<TelegramQueuePersistenceService> logger)
    {
        _logger = logger;
        _telegramQueue = telegramQueue;
        _telegramRepository = telegramRepository;
    }

    /// <summary>
    /// Reload all telegrams from the db and enqueue them.
    /// </summary>
    public async Task ReloadQueue()
    {
        ICollection<Telegram> telegrams = await _telegramRepository.GetAll();
        foreach (var telegram in telegrams.OrderBy(t => t.QueuedDate))
        {
            var conveyorTelegram = telegram.ToConveyorTelegram();
            _telegramQueue.Enqueue(conveyorTelegram);
        }
    }

    /// <summary>
    /// Store telegram when it was enqueued.
    /// </summary>
    public void OnItemEnqueued(object? sender, ConveyorTelegram telegram)
    {
        string? message = telegram.ToString();
        if (message == null || telegram.Func == nameof(Life))
        {
            _ = DeleteTelegram();
            return;
        }
        _logger.LogDebug("Persisting telegram {Telegram}", telegram.ToString());
        _ = StoreTelegram(message, telegram);
        _ = DeleteTelegram();
    }

    private async Task StoreTelegram(string message, ConveyorTelegram telegram)
    {
        var storedTelegram = Telegram.Create(message, telegram.Func);
        try
        {
            await _telegramRepository.GetByIdAsync(storedTelegram.Id);
        }
        catch (InvalidOperationException)
        {
            await _telegramRepository.AddAsync(storedTelegram);
        }
        catch (InvalidCastException ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    /// <summary>
    /// Delete telegram when it was dequeued.
    /// </summary>
    public void OnItemDequeued(object? sender, object telegram)
    {
        var conveyorTelegram = (ConveyorTelegram)telegram;
        string? message = conveyorTelegram.ToString();

        if (message == null  || conveyorTelegram.Func == nameof(Life))
        {
            return;
        }
        _logger.LogDebug("List telegram {Telegram} for deletion", telegram.ToString());

        var storedTelegram = Telegram.Create(message, conveyorTelegram.Func);
        _telegramsToDelete.Enqueue(storedTelegram);
    }

    private async Task DeleteTelegram()
    {
        if (_telegramsToDelete.TryDequeue(out var storedTelegram))
        {
            try
            {
                _logger.LogDebug("Try deleting telegram {Telegram}", storedTelegram.Id);
                storedTelegram = await _telegramRepository.GetByIdAsync(storedTelegram.Id);
                await _telegramRepository.RemoveAsync(storedTelegram);
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning("Could not remove telegram {Message} ", storedTelegram.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not remove telegram {Message} ", ex.Message);
            }
        }
    }

    /// <summary>
    /// Delete all remaining telegrams that were listed for deletion, but one
    /// in case restart of the service is shortly after dequeuing.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_telegramsToDelete.Count == 1)
        {
            return;
        }
        while (_telegramsToDelete.Count != 1)
        {
            await DeleteTelegram();
        }
    }
}