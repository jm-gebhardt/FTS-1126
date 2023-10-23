using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.ActionGroup;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.ActionGroup;

public class ActionGroupControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, ActionGroupController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    private readonly Mock<IActionGroupRetrievalService> _retrievalServiceMock = new();
    private readonly Mock<IActionGroupRegistry> _actionGroupRegistryMock = new();

    private readonly List<ActionGroupReadModel> _actionGroups = new();

    protected override ActionGroupController GetController()
    {
        IRequestClient<GetActionGroup> getActionGroupRequestClient = GetRequestClient<GetActionGroup>();
        var controller = new ActionGroupController(GetMapper(), getActionGroupRequestClient)
        {
            ControllerContext = new ControllerContext(
                new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ControllerActionDescriptor()))
        };
        return controller;
    }

    public ActionGroupControllerTestFixture<TRequest, TRequestHandler> Setup()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _retrievalServiceMock.Object);
            sc.AddTransient(_ => _actionGroupRegistryMock.Object);
        });

        _retrievalServiceMock.Setup(x => x.GetActionGroupWithNodesAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _actionGroups.FirstOrDefault(a => a.CorrelationId == c));
        return this;
    }

    public ActionGroupControllerTestFixture<TRequest, TRequestHandler> AddActionGroup(ActionGroupReadModel actionGroup)
    {
        _actionGroups.Add(actionGroup);
        return this;
    }

    public new ActionGroupControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        base.Build();

        return this;
    }
}