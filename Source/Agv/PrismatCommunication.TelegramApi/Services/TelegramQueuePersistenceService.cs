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
public class TelegramQueuePersistenceService: IAsyncDisposable
{
    private readonly ILogger<TelegramQueuePersistenceService> _logger;
    private readonly ObservableConcurrentQueue<PrismatTelegram> _telegramQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentQueue<Telegram> _telegramsToDelete = new();

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

        ICollection<Telegram> telegrams = await telegramRepository.GetAll();
        foreach (var telegram in telegrams.OrderBy(t => t.QueuedDate))
        {
            var prismatTelegram = telegram.ToPrismatTelegram();
            _telegramQueue.Enqueue(prismatTelegram);
        }
    }

    /// <summary>
    /// Store telegram and delete next telegram that was listed for deletion.
    /// </summary>
    public void OnItemEnqueued(object? sender, PrismatTelegram telegram)
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

    private async Task StoreTelegram(string message, PrismatTelegram telegram)
    {
        using var scope = _serviceProvider.CreateScope();

        var telegramRepository = scope.ServiceProvider.GetRequiredService<ITelegramRepository>();

        var storedTelegram = new Telegram(message, telegram.Func);
        try
        {
            await telegramRepository.GetByIdAsync(storedTelegram.Id);
        }
        catch (InvalidOperationException)
        {
            await telegramRepository.AddAsync(storedTelegram);
        }
    }

    /// <summary>
    /// List telegram for deletion.
    /// </summary>
    public void OnItemDequeued(object? sender, object telegram)
    {
        var prismatTelegram = (PrismatTelegram)telegram;
        string? message = prismatTelegram.ToString();
        if (message == null  || prismatTelegram.Func == nameof(Life))
        {
            return;
        }
        _logger.LogDebug("List telegram {Telegram} for deletion", telegram.ToString());

        var storedTelegram = new Telegram(message, prismatTelegram.Func);
        _telegramsToDelete.Enqueue(storedTelegram);
    }

    private async Task DeleteTelegram()
    {
        using var scope = _serviceProvider.CreateScope();

        var telegramRepository = scope.ServiceProvider.GetRequiredService<ITelegramRepository>();

        if (_telegramsToDelete.TryDequeue(out var storedTelegram))
        {
            try
            {
                _logger.LogDebug("Try deleting telegram {Telegram}", storedTelegram.Id);
                storedTelegram = await telegramRepository.GetByIdAsync(storedTelegram.Id);
                await telegramRepository.RemoveAsync(storedTelegram);
            }
            catch (InvalidOperationException)
            {
                _logger.LogDebug("Could not remove telegram {Message} ", storedTelegram.Message);
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