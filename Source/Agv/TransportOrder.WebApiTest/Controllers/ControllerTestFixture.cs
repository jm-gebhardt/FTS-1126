using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers;

public abstract class ControllerTestFixture<TRequest, TRequestHandler, TController> : MediatorTestFixture<TRequest, TRequestHandler>
    where TRequest : class
    where TRequestHandler : class, IConsumer
    where TController : Controller
{
    private IRequestClient<TRequest> GetRequestClient() => Mediator!.CreateRequestClient<TRequest>();

    public TController Controller { get; private set; } = null!;

    protected abstract TController GetController();

    protected IRequestClient<TClient> GetRequestClient<TClient>() where TClient : class => typeof(TRequest) == typeof(TClient) ? (IRequestClient<TClient>)GetRequestClient() : null!;

    protected IMapper GetMapper()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new AgvProfile());
            cfg.AddProfile(new AgvGraphProfile());
            cfg.AddProfile(new AgvPositionProfile());
            cfg.AddProfile(new MapProfile());
        });
        var mapper = new Mapper(mapperConfig);
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