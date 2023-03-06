using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
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
            var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile(new MappingProfile()); });
            var mapper = new Mapper(mapperConfig);
            return mapper;
        }

        protected readonly List<Action<IServiceCollection>> ServiceActions = new();
        protected override void ConfigureServices(IServiceCollection collection)
        {
            foreach (Action<IServiceCollection> serviceAction in ServiceActions)
            {
                serviceAction.Invoke(collection);
            }
        }

        protected override void Build()
        {
            base.Build();

            Controller = GetController();
            Controller.ControllerContext = new ControllerContext(
                new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ControllerActionDescriptor()));
        }
    }
}