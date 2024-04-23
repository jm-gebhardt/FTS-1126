using System.Linq.Expressions;
using AutoMapper;
using FluentValidation.Results;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.ActionGroup;

public class CreateActionGroupRequestHandlerTestFixture : MediatorTestFixture<CreatePickDropActionGroup, CreateActionGroupRequestHandler>
{
    private readonly Mock<IActionGroupRetrievalService> _retrievalService = new();
    public readonly Mock<IActionGroupRepository> ActionGroupRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IActionGroupValidationService> _actionGroupValidationService = new();

    protected override IEnumerable<Profile> GetProfiles() => [new ActionProfile()];

    public CreateActionGroupRequestHandlerTestFixture WithAgvExists(bool agvExists)
    {
        _agvDb.Setup(x => x.Agvs.ThrowIfAgvNotExistsAsync(It.IsAny<Guid>())).Callback((Guid c) =>
        {
            if (agvExists)
            {
                return;
            }
            throw new AgvNotFoundException(c);
        });
        return this;
    }

    public CreateActionGroupRequestHandlerTestFixture WithNodes(ICollection<GraphNode> nodes)
    {
        _agvDb.Setup(x => x.Graphs.GetNodesByIdsAsync(It.IsAny<ICollection<string>>())).ReturnsAsync(nodes);
        _agvDb.Setup(x => x.Graphs.GetNodesAsync(It.IsAny<Guid>(), It.IsAny<Expression<Func<GraphNode, bool>>?>())).ReturnsAsync(nodes);
        return this;
    }

    public CreateActionGroupRequestHandlerTestFixture WithValidationResult(ValidationResult result)
    {
        _actionGroupValidationService.Setup(x =>
                x.ValidateAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()))
            .ReturnsAsync(result);
        return this;
    }

    public new CreateActionGroupRequestHandlerTestFixture Build()
    {
        AddServices(s => s.AddTransient(_ => _agvDb.Object));
        AddServices(s => s.AddTransient(_ => _retrievalService.Object));
        AddServices(s => s.AddTransient(_ => ActionGroupRepository.Object));
        AddServices(s => s.AddTransient(_ => _actionGroupValidationService.Object));
        base.Build();
        ActionGroupRepository.Setup(x => x.AddAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>())).Verifiable();
        _retrievalService.Setup(x => x.EnrichActionGroupWithNodesAsync(It.IsAny<TransportOrder.Domain.Models.ActionGroup.ActionGroup>()))
            .ReturnsAsync((TransportOrder.Domain.Models.ActionGroup.ActionGroup ag) => ag.ConvertFromActionGroupWithFakeNodeType()!);
        return this;
    }
}