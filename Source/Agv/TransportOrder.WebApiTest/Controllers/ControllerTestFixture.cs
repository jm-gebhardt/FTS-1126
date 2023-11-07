using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using AgvPositionProfile = Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.AgvPositionProfile;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers;

public abstract class ControllerTestFixture<TRequest, TRequestHandler, TController> : MediatorTestFixture<TRequest, TRequestHandler>
    where TRequest : class
    where TRequestHandler : class, IConsumer
    where TController : Controller
{
    private IRequestClient<TRequest> GetRequestClient()
    {
        return Mediator!.CreateRequestClient<TRequest>();
    }

    public TController Controller { get; private set; } = null!;

    protected abstract TController GetController();

    protected IRequestClient<TClient> GetRequestClient<TClient>() where TClient : class
    {
        return typeof(TRequest) == typeof(TClient) ? (IRequestClient<TClient>)GetRequestClient() : null!;
    }

    protected IMapper GetMapper()
    {
        var mapper = MappingHelper.GetMapper(
            new AgvProfile(),
            new AgvGraphProfile(),
            new AgvPositionProfile(),
            new MapProfile(),
            new AgvErrorProfile(),
            new ActionProfile(),
            new Messages.Profiles.AgvProfile(),
            new Messages.Profiles.AgvGraphProfile(),
            new Messages.Profiles.AgvErrorProfile(),
            new Messages.Profiles.MapProfile(),
            new ActionGroupProfile()
        );
        return mapper;
    }

    public override MediatorTestFixture<TRequest, TRequestHandler> Build()
    {
        base.Build();

        Controller = GetController();
        Controller.ControllerContext = new ControllerContext(
            new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ControllerActionDescriptor()));

        return this;
    }
}