using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.RestrictedArea;

public class RestrictedAreaRequestHandlerTestFixture<TRequest>: MediatorTestFixture<TRequest, RestrictedAreaRequestHandler, MockConsumer>
where TRequest: class
{
    private readonly Mock<IRestrictedAreaRepository> _restrictedAreaRepository = new();

    public RestrictedAreaRequestHandlerTestFixture<TRequest> WithRestrictedArea(TransportOrder.Domain.Models.RestrictedArea.RestrictedArea restrictedArea)
    {
        _restrictedAreaRepository.Setup(x => x.GetRestrictedArea(restrictedArea.Id)).ReturnsAsync(restrictedArea);
        _restrictedAreaRepository.Setup(x => x.GetRestrictedAreas(It.IsAny<Guid[]>())).ReturnsAsync([restrictedArea]);
        return this;
    }

    public new RestrictedAreaRequestHandlerTestFixture<TRequest> Build()
    {
        AddServices(s => s.AddTransient(_ => _restrictedAreaRepository.Object));
        base.Build();
        return this;
    }
}