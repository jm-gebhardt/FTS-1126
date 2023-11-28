using Gebhardt.StoreWare.Agv.Common.MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

public class ResetAgvErrorsRequestHandlerTestFixture : MediatorTestFixture<ResetAgvErrors, ResetAgvErrorsRequestHandler>
{
    private readonly Mock<IAgvErrorRepository> _agvErrorRepository = new();
    private readonly Mock<IMessageAwaiter<AgvErrorChanged>> _agvErrorChangedAwaiter = new();
    private Action? _waitForMessageAction;

    public ResetAgvErrorsRequestHandlerTestFixture()
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
            s.AddTransient(_ => _agvErrorRepository.Object);
            s.AddTransient(_ => _agvErrorChangedAwaiter.Object);
        });
    }

    public ResetAgvErrorsRequestHandlerTestFixture WithActiveAgvErrors(IList<AgvError> errors)
    {
        _agvErrorRepository.Setup(x => x.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(errors);
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
}