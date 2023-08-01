using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Agvs;

[TestClass]
public class AgvReadyServiceTest
{
    [TestMethod]
    [DataRow(nameof(AgvStateMachine.Busy))]
    [DataRow(nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Charging))]
    [DataRow(nameof(AgvStateMachine.Discovered))]
    public async Task IsAgvCompleteAsync_WhenAgvStatusIsNull_ReturnsFalse(string state)
    {
        var fixture = new AgvReadyServiceTestFixture();
        var saga = AgvSagaGenerator.GenerateAgvSaga(state);
        saga.AgvStatus = null;

        bool isAgvComplete = await fixture.AgvReadyService.IsAgvCompleteAsync(saga);

        isAgvComplete.ShouldBeFalse();
    }
    
    [TestMethod]
    [DataRow(nameof(AgvStateMachine.Busy))]
    [DataRow(nameof(AgvStateMachine.Discovered))]
    public async Task IsAgvCompleteAsync_WhenAgvHasError_ReturnsFalse(string state)
    {
        var fixture = new AgvReadyServiceTestFixture();
        var saga = AgvSagaGenerator.GenerateAgvSaga(state);

        bool isAgvComplete = await fixture.AgvReadyService.IsAgvCompleteAsync(saga);

        isAgvComplete.ShouldBeFalse();
    }
    
    [TestMethod]
    [DataRow(nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Charging))]
    public async Task IsAgvCompleteAsync_WhenAgvHasError_ReturnsTrue(string state)
    {
        var fixture = new AgvReadyServiceTestFixture();
        var saga = AgvSagaGenerator.GenerateAgvSaga(state);

        bool isAgvComplete = await fixture.AgvReadyService.IsAgvCompleteAsync(saga);

        isAgvComplete.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task IsAgvCompleteAsync_WhenAgvIsChargingWithIgnoreChargingOption_ReturnsTrue()
    {
        var fixture = new AgvReadyServiceTestFixture();
        var saga = AgvSagaGenerator.GenerateAgvSaga(nameof(AgvStateMachine.Charging));

        bool isAgvComplete = await fixture.AgvReadyService.IsAgvCompleteAsync(saga, ignoreCharging:true);

        isAgvComplete.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task IsAgvCompleteAsync_WhenAgvIsChargingWithDontIgnoreChargingOption_ReturnsFalse()
    {
        var fixture = new AgvReadyServiceTestFixture();
        var saga = AgvSagaGenerator.GenerateAgvSaga(nameof(AgvStateMachine.Charging));

        bool isAgvComplete = await fixture.AgvReadyService.IsAgvCompleteAsync(saga, ignoreCharging: false);

        isAgvComplete.ShouldBeFalse();
    }
    
    [TestMethod]
    [DataRow(nameof(AgvStateMachine.Busy))]
    [DataRow(nameof(AgvStateMachine.Discovered))]
    public async Task IsAgvCompleteAsync_WhenAgvIsLockedWithIgnoreLockedOption_ReturnsFalse(string state)
    {
        var fixture = new AgvReadyServiceTestFixture();
        var saga = AgvSagaGenerator.GenerateAgvSaga(state);

        bool isAgvComplete = await fixture.AgvReadyService.IsAgvCompleteAsync(saga, ignoreLocked:true);

        isAgvComplete.ShouldBeFalse();
    }
    
    [TestMethod]
    [DataRow(nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Charging))]
    public async Task IsAgvCompleteAsync_WhenAgvIsLockedWithIgnoreLockedOption_ReturnsTrue(string state)
    {
        var fixture = new AgvReadyServiceTestFixture();
        var saga = AgvSagaGenerator.GenerateAgvSaga(state);

        bool isAgvComplete = await fixture.AgvReadyService.IsAgvCompleteAsync(saga, ignoreLocked:true);

        isAgvComplete.ShouldBeTrue();
    }
    
    [TestMethod]
    [DataRow(nameof(AgvStateMachine.Busy))]
    [DataRow(nameof(AgvStateMachine.Discovered))]
    public async Task IsAgvCompleteAsync_WhenAgvIsLockedWithDontIgnoreLockedOption_ReturnsFalse(string state)
    {
        var fixture = new AgvReadyServiceTestFixture();
        var saga = AgvSagaGenerator.GenerateAgvSaga(state);

        bool isAgvComplete = await fixture.AgvReadyService.IsAgvCompleteAsync(saga, ignoreLocked: false);

        isAgvComplete.ShouldBeFalse();
    }
    
    [TestMethod]
    [DataRow(nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Charging))]
    public async Task IsAgvCompleteAsync_WhenAgvIsLockedWithDontIgnoreLockedOption_ReturnsTrue(string state)
    {
        var fixture = new AgvReadyServiceTestFixture();
        var saga = AgvSagaGenerator.GenerateAgvSaga(state);

        bool isAgvComplete = await fixture.AgvReadyService.IsAgvCompleteAsync(saga, ignoreLocked: false);

        isAgvComplete.ShouldBeTrue();
    }
    
    [TestMethod]
    [DataRow(nameof(AgvStateMachine.Busy))]
    [DataRow(nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Charging))]
    [DataRow(nameof(AgvStateMachine.Discovered))]
    public async Task IsAgvCompleteAsync_WhenAgvIsReadyButAgvGraphDoesNotExist_ReturnsFalse(string state)
    {
        var saga = AgvSagaGenerator.GenerateAgvSaga(state);
        var fixture = new AgvReadyServiceTestFixture()
            .WithoutExistingAgvGraph(saga.CorrelationId);

        bool isAgvComplete = await fixture.AgvReadyService.IsAgvCompleteAsync(saga);

        isAgvComplete.ShouldBeFalse();
    }
}