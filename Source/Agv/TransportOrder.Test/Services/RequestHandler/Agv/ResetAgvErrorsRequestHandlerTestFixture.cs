using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.MassTransit;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.RequestHandlers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

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