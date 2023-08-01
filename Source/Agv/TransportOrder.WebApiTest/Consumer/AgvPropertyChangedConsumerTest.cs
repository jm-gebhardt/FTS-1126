using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer
{
    [TestClass]
    public class AgvPropertyChangedConsumerTest
    {
        private const string AgvId = "agv1";

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Receiving_AgvPropertyChanged_SendAgvStateToAllClients(bool alive)
        {
            var agv = new Core.States.AgvSaga(AgvId.ToGuid(), AgvId, nameof(AgvStateMachine.Idle)) { IsOnline = alive };
            
            var mapper = MappingHelper.GetMapper(new AgvProfile());
            var agvDto = mapper.Map<AgvDto>(agv);
            
            var mockGetAgvsClient = new Mock<IRequestClient<GetAgvsDtoRequest>>();
            var responseMock = new Mock<Response<GetAgvsDtoResult>>();
            responseMock.SetupGet(m => m.Message).Returns(new GetAgvsDtoResult(new[] { agvDto }));
            mockGetAgvsClient.Setup(
                    m => m.GetResponse<GetAgvsDtoResult>(It.IsAny<GetAgvsDtoRequest>(), default, default
                    ))
                .ReturnsAsync(responseMock.Object);
            
            var fixture = new ConsumerHubTestFixture<AgvPropertyChangedConsumer, IAgvHubContext>(services =>
            {
                services.AddSingleton(mockGetAgvsClient.Object);
            });
            await fixture.TestHarness!.Bus.Publish(
                new AgvPropertyChanged(
                    agv,
                    agv.CorrelationId
                ));

            (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
            (await fixture.ConsumerHarness!.Consumed.Any<AgvPropertyChanged>()).ShouldBeTrue();

            fixture.MockHub.Verify(h => h.SendAgvStateToAllClients(
                It.Is<AgvDto>(o => o.CorrelationId == agv.CorrelationId), false), Times.Once);
        }
    }
}
