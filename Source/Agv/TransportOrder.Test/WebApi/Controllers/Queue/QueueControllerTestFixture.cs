using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Queue;

public class QueueControllerTestFixture : MockRequestClientControllerTestFixture<QueueController>
{
    protected override IEnumerable<Profile> GetProfiles() => [new QueueProfile()];

    private TransportOrder.Domain.Models.Queue.Queue _queue = null!;
    
    protected override void SetupController()
    {
        AddRequestClient<DeleteQueue, Success>(() => new Success());
        AddRequestClient<GetQueue, GotQueue>(() => new GotQueue(_queue));
        AddRequestClient<TryAddQueue, GotQueue>(() => new GotQueue(_queue));
    }

    public new QueueControllerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public QueueControllerTestFixture WithQueue(TransportOrder.Domain.Models.Queue.Queue queue)
    {
        _queue = queue;
        return this;
    }
}