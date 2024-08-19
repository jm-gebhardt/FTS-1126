using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.RestrictedArea;

public class RestrictedAreaCommandHandlerTestFixture: MassTransitConsumerTestFixture<RestrictedAreaCommandHandler>
{
    private readonly Mock<IRestrictedAreaRepository> _restrictedAreaRepository = new();

    public RestrictedAreaCommandHandlerTestFixture()
    {
        AddServices(s => s.AddTransient(_ => _restrictedAreaRepository.Object));
        _restrictedAreaRepository.Setup(x => x.UpdateRestrictedArea(It.IsAny<TransportOrder.Domain.Models.RestrictedArea.RestrictedArea>())).Verifiable();
        _restrictedAreaRepository.Setup(x => x.AddRestrictedArea(It.IsAny<TransportOrder.Domain.Models.RestrictedArea.RestrictedArea>())).Verifiable();
        _restrictedAreaRepository.Setup(x => x.DeleteRestrictedArea(It.IsAny<Guid>())).Verifiable();
        Build();
    }

    public void VerifyRestrictedAreaAdded()
    {
        _restrictedAreaRepository.Verify(x => x.AddRestrictedArea(It.IsAny<TransportOrder.Domain.Models.RestrictedArea.RestrictedArea>()), Times.Once);
    }

    public void VerifyRestrictedAreaUpdated()
    {
        _restrictedAreaRepository.Verify(x => x.UpdateRestrictedArea(It.IsAny<TransportOrder.Domain.Models.RestrictedArea.RestrictedArea>()), Times.Once);
    }

    public void VerifyRestrictedAreaDeleted(Guid id)
    {
        _restrictedAreaRepository.Verify(x => x.DeleteRestrictedArea(id), Times.Once);
    }
}