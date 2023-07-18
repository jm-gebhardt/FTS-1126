using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Shouldly;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
    [TestClass]
    public class AgvControllerTest
    {
        [TestMethod]
        public async Task GetAgvs_Successfully()
        {
            AgvControllerTestFixture<GetAgvsRequest, GetAgvsRequestHandler> fixture = new AgvControllerTestFixture<GetAgvsRequest, GetAgvsRequestHandler>()
                .UseDefaultServices()
                .Build();

            var controller = fixture.Controller;
            ActionResult<IEnumerable<AgvDto>> response = await controller.GetAgvs();

            var result = response.Result as OkObjectResult;
            result.ShouldNotBeNull();

            var agvsIterable = result.Value as IList<AgvDto>;
            agvsIterable.ShouldNotBeNull();
            List<AgvDto> agvs = agvsIterable.ToList();
            agvs.Count.ShouldBe(fixture.AgvIds.Length);

            for (int index = 0; index < fixture.AgvIds.Length; index++)
            {
                string agvId = fixture.AgvIds[index];
                string state = fixture.States[index];

                agvs[index].CorrelationId.ShouldBe(agvId.ToGuid());
                agvs[index].AgvId.ShouldBe(agvId);
                Enum.TryParse(state, out AgvState agvState);
                agvs[index].CurrentState.ShouldBe(agvState);
                agvs[index].Errors.ShouldBeEmpty();
            }
        }

        [TestMethod]
        public async Task GetAgvs_WhenAgvHasAgvError_ReturnsAgvWithAgvErrorList()
        {
            var fixture = new AgvControllerTestFixture<GetAgvsRequest, GetAgvsRequestHandler>();
            List<AgvError> expectedAgvErrors = fixture.AgvIds.Select(agvId => AgvErrorGenerator.GenerateAgvError(agvId.ToGuid())).ToList();

            fixture.UseDefaultServices()
                .WithActiveAgvErrors(expectedAgvErrors)
                .Build();

            var controller = fixture.Controller;
            ActionResult<IEnumerable<AgvDto>> response = await controller.GetAgvs();

            var result = response.Result as OkObjectResult;
            result.ShouldNotBeNull();

            var agvsIterable = result.Value as IList<AgvDto>;
            agvsIterable.ShouldNotBeNull();
            List<AgvDto> agvs = agvsIterable.ToList();
            agvs.Count.ShouldBe(fixture.AgvIds.Length);

            for (int index = 0; index < fixture.AgvIds.Length; index++)
            {
                agvs[index].Errors.ShouldNotBeEmpty();
                foreach (var agvDtoError in agvs[index].Errors)
                {
                    agvDtoError.AgvCorrelationId.ShouldBe(agvDtoError.AgvCorrelationId);
                    var expectedAgvError = expectedAgvErrors.Find(e => e.ErrorType == agvDtoError.ErrorType);
                    expectedAgvError.ShouldNotBeNull();
                    agvDtoError.ErrorDescription.ShouldBe(expectedAgvError.ErrorDescription);
                    agvDtoError.ErrorLevel.ShouldBe(expectedAgvError.ErrorLevel);
                    agvDtoError.ErrorType.ShouldBe(expectedAgvError.ErrorType);
                    agvDtoError.ErrorReferences.ShouldBe(expectedAgvError.ErrorReferences);
                    agvDtoError.ErrorReceived.ShouldBe(expectedAgvError.ErrorReceived);
                    agvDtoError.CorrelationId.ShouldBe(expectedAgvError.CorrelationId);
                    agvDtoError.ErrorResolved.ShouldBe(expectedAgvError.ErrorResolved);
                }
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public async Task GetAgvGraph_Successfully(int index)
        {
            var fixture = new AgvControllerTestFixture<GetAgvGraphRequest, GetAgvGraphRequestHandler>();
            var correlationId = fixture.AgvIds[index].ToGuid();
            var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
            
            fixture
                .WithAgvGraph(correlationId, agvGraph)
                .UseDefaultServices()
                .Build();
            
            var controller = fixture.Controller;
            ActionResult<GraphDto> response = await controller.GetAgvGraph(correlationId);
            var result = response.Result as OkObjectResult;
            result.ShouldNotBeNull();

            var graph = result.Value as GraphDto;
            graph.ShouldNotBeNull();

            graph.CorrelationId.ShouldBe(correlationId);
            graph.Nodes.Count.ShouldBe(agvGraph.Nodes.Count);
            graph.Edges.Count.ShouldBe(agvGraph.Edges.Count);

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                graph.Nodes[i].ShouldSatisfyAllConditions(
                    n => n.Id.ShouldBe(agvGraph.Nodes.ElementAt(i).Id),
                    n => n.X.ShouldBe(agvGraph.Nodes.ElementAt(i).X),
                    n => n.Y.ShouldBe(agvGraph.Nodes.ElementAt(i).Y),
                    n => n.Z.ShouldBe(agvGraph.Nodes.ElementAt(i).Z),
                    n => n.Theta.ShouldBe(agvGraph.Nodes.ElementAt(i).Theta),
                    x => x.Type.ToString().ShouldBe(agvGraph.Nodes.ElementAt(i).Type.ToString()));
            }

            for (int i = 0; i < graph.Edges.Count; i++)
            {
                graph.Edges[i].ShouldSatisfyAllConditions(
                    e => e.From.Id.ShouldBe(agvGraph.Edges.ElementAt(i).FromNodeId),
                    e => e.To.Id.ShouldBe(agvGraph.Edges.ElementAt(i).ToNodeId));
            }
        }

        [TestMethod]
        public async Task GetAgvGraph_NotFoundAsync()
        {
            AgvControllerTestFixture<GetAgvGraphRequest, GetAgvGraphRequestHandler> fixture = new AgvControllerTestFixture<GetAgvGraphRequest, GetAgvGraphRequestHandler>()
                .UseDefaultServices()
                .Build();
            var controller = fixture.Controller;
            var correlationId = Guid.Empty;

            await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.GetAgvGraph(correlationId));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public async Task GetAgvData_Successfully(int index)
        {
            AgvControllerTestFixture<GetAgvDataRequest, GetAgvDataRequestHandler> fixture = new AgvControllerTestFixture<GetAgvDataRequest, GetAgvDataRequestHandler>()
                .UseDefaultServices()
                .Build();
            var controller = fixture.Controller;
            var correlationId = fixture.AgvIds[index].ToGuid();
            ActionResult<AgvDataDto> response = await controller.GetAgvData(correlationId);
            var result = response.Result as OkObjectResult;
            result.ShouldNotBeNull();

            var agvData = result.Value as AgvDataDto;
            agvData.ShouldNotBeNull();

            agvData.CorrelationId.ShouldBe(correlationId);
            agvData.IpV4Address.ShouldBe(fixture.Ips[index]);
        }

        [TestMethod]
        public async Task GetAgvData_NotFound()
        {
            AgvControllerTestFixture<GetAgvDataRequest, GetAgvDataRequestHandler> fixture = new AgvControllerTestFixture<GetAgvDataRequest, GetAgvDataRequestHandler>()
                .UseDefaultServices()
                .Build();
            var controller = fixture.Controller;
            var correlationId = Guid.Empty;

            await MediatorShould.ThrowRequestExceptionAsync<AgvDataNotFoundException>(async () => await controller.GetAgvData(correlationId));
        }

        [TestMethod]
        public async Task UnlockAgv_NotFound()
        {
            AgvControllerTestFixture<UnlockAgvCommand, UnlockAgvCommandHandler> fixture = new AgvControllerTestFixture<UnlockAgvCommand, UnlockAgvCommandHandler>()
                .UseDefaultServices()
                .Build();
            var controller = fixture.Controller;
            var correlationId = Guid.Empty;

            Should.Throw<AggregateException>(() => controller.UnlockAgv(correlationId).Result)
                .GetBaseException().GetBaseException().ShouldBeOfType<AgvNotFoundException>();

            await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.UnlockAgv(correlationId));
        }

        [TestMethod]
        public async Task UnlockAgv_WhenNotInDiscovered_UnlocksAgv()
        {
            AgvControllerTestFixture<UnlockAgvCommand, UnlockAgvCommandHandler> fixture = new AgvControllerTestFixture<UnlockAgvCommand, UnlockAgvCommandHandler>()
                .UseDefaultServices()
                .Build();
            var controller = fixture.Controller;
            var correlationId = fixture.AgvIds[1].ToGuid(); // AGV not in state 'Discovered'

            var result = await controller.UnlockAgv(correlationId) as OkResult;
            result.ShouldNotBeNull();

            var harness = fixture.TestHarness!;
            (await harness.Consumed.Any<UnlockAgvCommand>()).ShouldBeTrue();
            (await harness.Published.Any<UnlockAgvCommand>()).ShouldBeTrue();
        }

        [TestMethod]
        public async Task UnlockAgv_Ok()
        {
            AgvControllerTestFixture<UnlockAgvCommand, UnlockAgvCommandHandler> fixture = new AgvControllerTestFixture<UnlockAgvCommand, UnlockAgvCommandHandler>()
                .UseDefaultServices()
                .Build();
            var controller = fixture.Controller;
            var correlationId = fixture.AgvIds[2].ToGuid();

            var result = await controller.UnlockAgv(correlationId) as OkResult;
            result.ShouldNotBeNull();

            var harness = fixture.TestHarness!;
            (await harness.Consumed.Any<UnlockAgvCommand>()).ShouldBeTrue();
            (await harness.Published.Any<UnlockAgvCommand>()).ShouldBeTrue();
        }

        [TestMethod]
        public async Task LockAgv_WhenAgvRegistryThrows_ThrowsAgvNotFoundException()
        {
            AgvControllerTestFixture<LockAgvCommand, LockAgvCommandHandler> fixture = new AgvControllerTestFixture<LockAgvCommand, LockAgvCommandHandler>()
                .UseDefaultServices()
                .Build();

            var controller = fixture.Controller;
            var correlationId = Guid.Empty;
            fixture.AgvRegistry.Setup(x => x.GetRequiredAgv(It.Is<Guid>(g => g == correlationId))).Returns(() => throw new AgvNotFoundException(correlationId));

            await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.LockAgv(correlationId));
        }

        [TestMethod]
        public async Task LockAgv_Ok()
        {
            AgvControllerTestFixture<LockAgvCommand, LockAgvCommandHandler> fixture = new AgvControllerTestFixture<LockAgvCommand, LockAgvCommandHandler>()
                .UseDefaultServices()
                .Build();
            var controller = fixture.Controller;
            var correlationId = fixture.AgvIds[2].ToGuid();

            var result = await controller.LockAgv(correlationId) as OkResult;
            result.ShouldNotBeNull();

            var harness = fixture.TestHarness!;
            (await harness.Consumed.Any<LockAgvCommand>()).ShouldBeTrue();
            (await harness.Published.Any<LockAgvCommand>()).ShouldBeTrue();
        }

        [TestMethod]
        public async Task EmitRequestAgvData_NotFound()
        {
            AgvControllerTestFixture<RequestAgvDataCommand, RequestAgvDataCommandHandler> fixture = new AgvControllerTestFixture<RequestAgvDataCommand, RequestAgvDataCommandHandler>()
                .UseDefaultServices()
                .Build();
            var controller = fixture.Controller;
            var correlationId = Guid.Empty;

            await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.EmitRequestData(correlationId));
        }

        [TestMethod]
        public async Task EmitRequestAgvData_Ok()
        {
            AgvControllerTestFixture<RequestAgvDataCommand, RequestAgvDataCommandHandler> fixture = new AgvControllerTestFixture<RequestAgvDataCommand, RequestAgvDataCommandHandler>()
                .UseDefaultServices()
                .Build();
            var controller = fixture.Controller;
            var correlationId = fixture.AgvIds[2].ToGuid();

            var result = await controller.EmitRequestData(correlationId) as OkResult;
            result.ShouldNotBeNull();

            var harness = fixture.TestHarness!;
            (await harness.Consumed.Any<RequestAgvDataCommand>()).ShouldBeTrue();
            (await harness.Published.Any<RequestAgvDataCommand>()).ShouldBeTrue();
        }

        [TestMethod]
        public async Task EmitRequestAgvGraph_NotFound()
        {
            AgvControllerTestFixture<RequestAgvGraphCommand, RequestAgvGraphCommandHandler> fixture = new AgvControllerTestFixture<RequestAgvGraphCommand, RequestAgvGraphCommandHandler>()
                .UseDefaultServices()
                .Build();
            var controller = fixture.Controller;
            var correlationId = Guid.Empty;

            await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.EmitRequestGraph(correlationId));
        }

        [TestMethod]
        public async Task EmitRequestAgvGraph_Ok()
        {
            AgvControllerTestFixture<RequestAgvGraphCommand, RequestAgvGraphCommandHandler> fixture = new AgvControllerTestFixture<RequestAgvGraphCommand, RequestAgvGraphCommandHandler>()
                .UseDefaultServices()
                .Build();
            var controller = fixture.Controller;
            var correlationId = fixture.AgvIds[2].ToGuid();

            var result = await controller.EmitRequestGraph(correlationId) as OkResult;
            result.ShouldNotBeNull();

            var harness = fixture.TestHarness!;
            (await harness.Consumed.Any<RequestAgvGraphCommand>()).ShouldBeTrue();
            (await harness.Published.Any<RequestAgvGraphCommand>()).ShouldBeTrue();
        }
    }
}