using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using AgvTypeModel = Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes.AgvType;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.AgvType;

public class AgvTypeCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvTypeCommandHandler>
{
    public const string AgvTypeIdentifier = "Type";

    private Mock<IAgvDb> AgvDb { get; } = new();

    private Mock<ILoggingService> LoggingService { get; } = new();

    protected override IEnumerable<Profile> GetProfiles()
    {
        return [new AgvTypeProfile()];
    }

    public AgvTypeCommandHandlerTestFixture()
    {
        AgvDb.Setup(x => x.AgvTypes.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        AgvDb.Setup(x => x.AgvTypes.AddOrUpdateAsync(It.IsAny<AgvTypeModel>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Build();
    }

    protected override MassTransitConsumerTestFixture<AgvTypeCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => AgvDb.Object);
        collection.AddTransient(_ => LoggingService.Object);
        return this;
    }

    public AgvTypeCommandHandlerTestFixture WithExistingAgvType(string agvTypeIdentifier)
    {
        AgvDb.Setup(x => x.AgvTypes.ExistsAsync(agvTypeIdentifier)).ReturnsAsync(true);
        return this;
    }

    public void VerifyAgvTypeUpserted(Times times)
    {
        AgvDb.Verify(x => x.AgvTypes.AddOrUpdateAsync(It.IsAny<AgvTypeModel>(), It.IsAny<CancellationToken>()), times);
    }

    public void VerifySystemLogged(Times times)
    {
        LoggingService.Verify(
            x => x.LogSystem(It.IsAny<ILogger>(),
                LogCodes.SystemAgvTypeCreated,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<LogLevel>()),
            times);
    }
}