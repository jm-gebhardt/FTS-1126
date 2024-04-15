using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.MassTransit;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.Common.Testing.RequestHandlers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.RequestHandler.Agv;

public class ResetAgvErrorsRequestHandlerTestFixture : MediatorTestFixture<ResetAgvErrors, ResetAgvErrorsRequestHandler>
{
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IMessageAwaiter<AgvErrorChanged>> _agvErrorChangedAwaiter = new();
    private Action? _waitForMessageAction;

    public ResetAgvErrorsRequestHandlerTestFixture(bool mockBus = false)
    {
        _agvErrorChangedAwaiter.Setup(x => x.WaitAsync(It.IsAny<Guid>(), It.IsAny<Func<AgvErrorChanged, bool>>(), It.IsAny<CancellationToken>(), It.IsAny<Func<Task>>(), It.IsAny<bool>()))
            .Callback<Guid, Func<AgvErrorChanged, bool>, CancellationToken, Func<Task>, bool>((_, _, _, executeAfterListeningTask, _) =>
            {
                // unfortunately there is no async callback in Mock, so the only option to handle this task here is to wait for it synchronously and hope it works.
                executeAfterListeningTask?.Invoke().Wait();
                _waitForMessageAction?.Invoke();
            });

        AddServices(s =>
        {
            s.AddTransient(_ => _agvDb.Object);
            s.AddTransient(_ => _agvErrorChangedAwaiter.Object);
        });
        
        if (mockBus)
        {
            var busMock = new Mock<IBus>();
            AddServices(s => s.AddTransient(_ => busMock.Object));
        }
        else
        { 
            AddConsumers<SuccessRequestHandlerMock<ResetAgvErrors>>();
        }
    }

    public ResetAgvErrorsRequestHandlerTestFixture WithActiveAgvErrors(IList<AgvError> errors)
    {
        _agvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(errors);
        return this;
    }

    public ResetAgvErrorsRequestHandlerTestFixture WithWaitForMessageAction(Action waitForMessageAction)
    {
        _waitForMessageAction = waitForMessageAction;
        return this;
    }

    public new ResetAgvErrorsRequestHandlerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public ResetAgvErrorsRequestHandlerTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvDb.AddAgvsToMockRepository(agv);
        return this;
    }
}