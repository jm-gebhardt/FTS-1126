using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
    [TestClass]
    public class AgvActionControllerTest
    {
        [TestMethod]
        public void When_ActionRegistryEmpty_EmptyList()
        {
            var fixture = new AgvActionControllerTestFixture<GetAgvActionGroupsRequest, GetAgvActionGroupsRequestHandler>();
            fixture.UseDefaultServices();
            
        }
    }
    
}
