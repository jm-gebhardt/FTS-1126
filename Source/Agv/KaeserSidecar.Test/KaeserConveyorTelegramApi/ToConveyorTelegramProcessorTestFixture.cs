using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi;

public class ToConveyorTelegramProcessorTestFixture: TelegramProcessorTestFixture<ToConveyorTelegramProcessor, ConveyorTelegram>
{
#pragma warning disable CA2211
    [UsedImplicitly]
    public static bool EnqueueEventTriggered;
 
    public static bool DequeueEventTriggered;
#pragma warning restore CA2211
    private readonly ObservableConcurrentQueue<ConveyorTelegram> _queue;

    public ToConveyorTelegramProcessorTestFixture():base(null)
    {
        AddServices(s => s.AddSingleton(_ => new ObservableConcurrentQueue<ConveyorTelegram>()));
        Build();
        _queue = Provider.GetRequiredService<ObservableConcurrentQueue<ConveyorTelegram>>();
        _queue.ItemEnqueued += OnTelegramEnqueued;
        _queue.ItemDequeued += OnTelegramDequeued;
    }

    [PublicAPI]
    public  static void OnTelegramEnqueued(object? sender, ConveyorTelegram telegram)
    {
        EnqueueEventTriggered = true;
    }

    [PublicAPI]
    public static void OnTelegramDequeued(object? sender, ConveyorTelegram telegram)
    {
        DequeueEventTriggered = true;
    }
}