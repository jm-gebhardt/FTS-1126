using AutoMapper;
using FluentValidation.Results;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Linq.Expressions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.ActionGroup;

public class CreateActionGroupRequestHandlerTestFixture : MediatorTestFixture<CreatePickDropActionGroup, CreateActionGroupRequestHandler>
{
    private readonly Mock<IActionGroupRetrievalService> _retrievalService = new();
    public readonly Mock<IActionGroupRepository> ActionGroupRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IActionGroupValidationService> _actionGroupValidationService = new();

    private static Mapper GetMapper()
    {
        return MappingHelper.GetMapper(new ActionProfile());
    }

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
        AddServices(s => s.AddTransient<IMapper>(_ => GetMapper()));
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