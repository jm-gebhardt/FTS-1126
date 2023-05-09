using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using MassTransit;
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
            MapRequestTestFixture<GetMapListRequest, GetMapListRequestHandler> fixture = new MapRequestTestFixture<GetMapListRequest, GetMapListRequestHandler>()
                .UseDefaultServices()
                .Build();

            Response<GetMapListResult> response = await fixture.GetResponseAsync<GetMapListResult>(new GetMapListRequest());
            List<MapDto> maps = response.Message.Maps;

            maps.ShouldNotBeEmpty();

            var harness = fixture.TestHarness!;

            (await harness.Consumed.Any<GetMapListRequest>()).ShouldBeTrue();
            (await harness.Consumed.Any<GetMapListResult>()).ShouldBeTrue();

            (await harness.Published.Any<GetMapListRequest>()).ShouldBeTrue();
            (await harness.Published.Any<GetMapListResult>()).ShouldBeTrue();
        }
    }
}