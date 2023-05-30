using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Actions.Handlers.Queries;

public class ActionRequestHandlerTestFixture<T> : MassTransitTestFixture<T>
    where T : class, IConsumer
{

    public readonly Mock<IActionGroupRegistry> ActionRegistryMock = new ();

    public ActionGroupState[] AllActionGroupStates => Enum.GetValues(typeof(ActionGroupState)).Cast<ActionGroupState>().ToArray();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        base.ConfigureServices(collection);
        collection.AddTransient<IActionGroupRegistry>(_ => ActionRegistryMock.Object);
        collection.AddTransient<IMapper>(_ => Mock.Of<IMapper>());
    }

    public async void ShouldContainFault<TMessage, TException>()
    {
        (await TestHarness!.Published.Any<Fault<TMessage>>()).ShouldBeTrue();

        IPublishedMessage<Fault<TMessage>>? fault = TestHarness.Published.Select<Fault<TMessage>>().First();
        fault.Context.Message.Exceptions.ShouldContain(e => e.ExceptionType == typeof(TException).FullName);
    }

    public ActionGroup CreatePickDropActionGroup(ActionState state)
    {
        var correlationId = Guid.NewGuid();
        var group = new ActionGroup(Guid.NewGuid(),
            "${correlationId}-pick-drop",
            null,
            DateTime.MinValue, 
            null,
            null,
            state,
            new List<AgvAction>() { 
                new(Guid.NewGuid(), $"{correlationId}-pick", ActionType.Pick, correlationId, "S1"), 
                new (Guid.NewGuid(), $"{correlationId}-drop", ActionType.Drop, correlationId, "D2") 
            });

        switch (state)
        {
            case ActionState.Created:
                group.Actions[0].State = ActionState.Created;
                group.Actions[1].State = ActionState.Created;
                break;
            
            case ActionState.Started:
                group.Actions[0].State = ActionState.Started;
                group.Actions[1].State = ActionState.Assigned;
                break;
            
            case ActionState.Assigned:
                group.Actions[0].State = ActionState.Assigned;
                group.Actions[1].State = ActionState.Assigned;
                break;

            case ActionState.Completed:
                group.Actions[0].State = ActionState.Completed;
                group.Actions[1].State = ActionState.Completed;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
        return group;
    }
}