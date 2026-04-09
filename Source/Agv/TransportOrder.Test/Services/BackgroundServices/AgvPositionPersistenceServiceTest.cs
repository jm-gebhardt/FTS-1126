using Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.BackgroundServices;

[TestClass]
public class AgvPositionPersistenceServiceTest
{
    private static (AgvPositionPersistenceService Service, Mock<IAgvPositionRepository> RepoMock) CreateService()
    {
        var repoMock = new Mock<IAgvPositionRepository>();
        repoMock.Setup(x => x.GetAllPositions()).Returns([]);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var scope = new Mock<IServiceScope>();
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAgvPositionRepository))).Returns(repoMock.Object);
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);
        var logger = new Mock<ILogger<AgvPositionPersistenceService>>();
        var service = new AgvPositionPersistenceService(scopeFactory.Object, logger.Object);
        return (service, repoMock);
    }

    [TestMethod]
    public async Task StopAsync_CallsPersistAllPositionsAsync()
    {
        (var service, Mock<IAgvPositionRepository> repoMock) = CreateService();

        await service.StopAsync(CancellationToken.None);

        repoMock.Verify(x => x.PersistAllPositionsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task StopAsync_PassesCancellationToken()
    {
        (var service, Mock<IAgvPositionRepository> repoMock) = CreateService();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await service.StopAsync(token);

        repoMock.Verify(x => x.PersistAllPositionsAsync(token), Times.Once);
    }

    [TestMethod]
    public async Task StopAsync_WhenPersistThrows_DoesNotRethrow()
    {
        (var service, Mock<IAgvPositionRepository> repoMock) = CreateService();
        repoMock.Setup(x => x.PersistAllPositionsAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("DB failure"));

        await Should.NotThrowAsync(() => service.StopAsync(CancellationToken.None));
    }
}
