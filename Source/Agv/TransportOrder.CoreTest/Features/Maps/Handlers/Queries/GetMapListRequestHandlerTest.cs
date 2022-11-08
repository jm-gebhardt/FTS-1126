using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Maps.Handlers.Queries
{
    [TestClass]
    public class GetMapListRequestHandlerTest
    {
        [TestMethod]
        public async Task GetMaps_ShouldReturnListOfMaps_WhereMapRegistryIsContained()
        {
            var mapTestFixture = new MapRequestHandlerTestFixture();
            var request = new GetMapListRequest();
            var handler = new GetMapListRequestHandler(mapTestFixture.AgvRegistryMock.Object, mapTestFixture.MapRegistryMock.Object, mapTestFixture.Mapper);

            List<MapDto> maps = await handler.Handle(request, CancellationToken.None);

            maps.ShouldNotBeEmpty();
        }
    }
}