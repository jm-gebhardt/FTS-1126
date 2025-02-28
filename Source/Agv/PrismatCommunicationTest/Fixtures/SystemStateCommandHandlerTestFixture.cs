using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.SystemStatus;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Services.CommandHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class SystemStateCommandHandlerTestFixture: MassTransitConsumerTestFixture<SystemStatusCommandHandler>, IDisposable
{
    private readonly Mock<IHostCommunicator> _hostCommunicatorMock = new();
    private readonly Mock<ISystemStatusRepository> _systemStateRepositoryMock = new();

    protected override MassTransitConsumerTestFixture<SystemStatusCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _systemStateRepositoryMock.Object);
        collection.AddTransient(_ => _hostCommunicatorMock.Object);
        return this;
    }

    public SystemStateCommandHandlerTestFixture()
    {
        _systemStateRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<SystemStatus>())).Verifiable();
        Build();
    }

    public void VerifyReportSystemStatus(bool isReady , Times times = default)
    {
        _hostCommunicatorMock.Verify(x => x.ReportSystemStatus(isReady, It.IsAny<int>(),  It.IsAny<string>(), It.IsAny<DateTime>()), times);
    }

    public SystemStateCommandHandlerTestFixture WithSystemStatus(SystemStatus? status)
    {
        _systemStateRepositoryMock.Setup(x => x.GetStatus()).ReturnsAsync(status);
        return this;
    }

    public void Dispose()
    {
        _hostCommunicatorMock.Invocations.Clear();
    }
}