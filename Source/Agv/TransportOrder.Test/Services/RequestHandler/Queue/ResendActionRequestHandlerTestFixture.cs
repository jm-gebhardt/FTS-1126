using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Queue;

public class ResendActionRequestHandlerTestFixture<TMessage> : MediatorTestFixture<TMessage, ResendActionRequestHandler, MockConsumer>
    where TMessage : class
{
    private readonly Mock<IAgvDb> _agvDb = new();

    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();

    protected override IEnumerable<Profile> GetProfiles() => [new Gebhardt.Acc.TransportOrder.Domain.Profiles.ActionProfile()];

    protected override ResendActionRequestHandlerTestFixture<TMessage> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _agvDb.Object);
        collection.AddTransient(_ => _actionGroupRepository.Object);

        return this;
    }

    public new ResendActionRequestHandlerTestFixture<TMessage> Build()
    {
        base.Build();
        return this;
    }

    public ResendActionRequestHandlerTestFixture<TMessage> AddAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        _agvDb.AddAgvs(agvs);
        return this;
    }

    public ResendActionRequestHandlerTestFixture<TMessage> AddActionGroup(Common.Messages.ActionGroups.ActionGroup? actionGroup)
    {
        var ag = Mapper.Map<TransportOrder.Domain.Models.ActionGroup.ActionGroup>(actionGroup);
        _actionGroupRepository.Setup(x => x.GetCurrentAssignedActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(ag);

        return this;
    }
}