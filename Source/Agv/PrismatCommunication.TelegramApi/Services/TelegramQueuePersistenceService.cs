using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.Telegram;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Services;

/// <summary>
/// Persists the telegram queue.
/// </summary>
public class TelegramQueuePersistenceService : IAsyncDisposable
{
    private readonly ILogger<TelegramQueuePersistenceService> _logger;
    private readonly ObservableConcurrentQueue<PrismatTelegram> _telegramQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentQueue<Telegram> _telegramsToDelete = new();
    private readonly ConcurrentDictionary<Guid, byte> _storedTelegramIds = new();
    private volatile bool _isReloading;

    /// <inheritdoc cref="TelegramQueuePersistenceService"/>>
    public TelegramQueuePersistenceService(ObservableConcurrentQueue<PrismatTelegram> telegramQueue, IServiceProvider serviceProvider, ILogger<TelegramQueuePersistenceService> logger)
    {
        _logger = logger;
        _telegramQueue = telegramQueue;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Reload all telegrams from the db and enqueue them.
    /// </summary>
    public async Task ReloadQueue()
    {
        using var scope = _serviceProvider.CreateScope();

        var telegramRepository = scope.ServiceProvider.GetRequiredService<ITelegramRepository>();

        var telegrams = await telegramRepository.GetAll();
        _isReloading = true;
        foreach (var telegram in telegrams.OrderBy(t => t.QueuedDate))
        {
            _storedTelegramIds.TryAdd(telegram.Id, 0);
            var prismatTelegram = telegram.ToPrismatTelegram();
            _telegramQueue.Enqueue(prismatTelegram);
        }
        _isReloading = false;
    }

    /// <summary>
    /// Store telegram and delete next telegram that was listed for deletion.
    /// </summary>
    public async Task OnTelegramEnqueued(PrismatTelegram telegram)
    {
        if (telegram.ToString() == null || telegram.Func == nameof(Life))
        {
            await DeleteTelegrams();
            return;
        }
        _logger.LogDebug("Persisting telegram {Telegram}", telegram.ToString());
        await StoreTelegram(telegram);
        await DeleteTelegrams();
    }

    private async Task StoreTelegram(PrismatTelegram telegram)
    {
        if (_isReloading)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();

        var telegramRepository = scope.ServiceProvider.GetRequiredService<ITelegramRepository>();

        var storedTelegram = new Telegram(telegram.ToStringWithoutSeq(), telegram.Func);
        try
        {
            await telegramRepository.GetByIdAsync(storedTelegram.Id);
        }
        catch (InvalidOperationException)
        {
            await telegramRepository.AddAsync(storedTelegram);
        }
        _storedTelegramIds.TryAdd(storedTelegram.Id, 0);
    }

    /// <summary>
    /// Delete telegram from repo after it has been acknowledged by the receiver.
    /// </summary>
    public async Task OnTelegramAcknowledged(PrismatTelegram prismatTelegram)
    {
        if (prismatTelegram.ToString() == null || prismatTelegram.Func == nameof(Life))
        {
            return;
        }
        _logger.LogDebug("Deleting acknowledged telegram {Telegram}", prismatTelegram.ToString());

        var storedTelegram = new Telegram(prismatTelegram.ToStringWithoutSeq(), prismatTelegram.Func);
        _telegramsToDelete.Enqueue(storedTelegram);
        await DeleteTelegrams();
    }

    private async Task DeleteTelegrams()
    {
        using var scope = _serviceProvider.CreateScope();

        var telegramRepository = scope.ServiceProvider.GetRequiredService<ITelegramRepository>();

        while (_telegramsToDelete.TryDequeue(out var storedTelegram))
        {
            if (!_storedTelegramIds.ContainsKey(storedTelegram.Id))
            {
                // Store has not completed yet. Put it back and stop.
                _telegramsToDelete.Enqueue(storedTelegram);
                break;
            }

            try
            {
                _logger.LogDebug("Try deleting telegram {Telegram}", storedTelegram.Id);
                storedTelegram = await telegramRepository.GetByIdAsync(storedTelegram.Id);
                await telegramRepository.RemoveAsync(storedTelegram);
                _storedTelegramIds.TryRemove(storedTelegram.Id, out _);
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning("Could not remove telegram {Message}, not found in DB", storedTelegram.Message);
                _storedTelegramIds.TryRemove(storedTelegram.Id, out _);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete telegram {Message} — re-queuing for retry", storedTelegram.Message);
                _telegramsToDelete.Enqueue(storedTelegram);
                break;
            }
        }
    }

    /// <summary>
    /// Delete all remaining telegrams that were listed for deletion.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_telegramsToDelete.IsEmpty)
        {
            return;
        }
        await DeleteTelegrams();
    }
}