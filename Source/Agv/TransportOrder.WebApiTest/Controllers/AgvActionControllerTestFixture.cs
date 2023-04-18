using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
    public class AgvActionControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, AgvActionController>
        where TRequest : class
        where TRequestHandler : class, IConsumer
    {
        public AgvActionControllerTestFixture<TRequest, TRequestHandler> UseDefaultServices()
        {
            
            AddServices(sc => sc.AddTransient(_ => GetAgvActionRegistry().Object));
            return this;
        }
        
        protected override AgvActionController GetController()
        {
            IRequestClient<GetAgvActionGroupsRequest> getAgvActionGroupsRequest = GetRequestClient<GetAgvActionGroupsRequest>();
            IRequestClient<GetAgvActionGroupCountsByStateRequest> getAgvActionGroupCountsByStateRequest = GetRequestClient<GetAgvActionGroupCountsByStateRequest>();
            
            var controller = new AgvActionController(getAgvActionGroupsRequest, getAgvActionGroupCountsByStateRequest)
            {
                ControllerContext = new ControllerContext(
                    new ActionContext(
                        new DefaultHttpContext(),
                        new RouteData(),
                        new ControllerActionDescriptor()
                        )
                    )
            };
            return controller;
        }

        private Mock<IAgvActionRegistry> GetAgvActionRegistry()
        {
            var registry = new Mock<IAgvActionRegistry>();
            
            return registry;
        }
    }
    
 
}


