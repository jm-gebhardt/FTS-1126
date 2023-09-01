using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

[TestClass]
public class AgvErrorRegistryTest
{
    private static void VerifyAgvErrorsInRegistry(IList<AgvError>? agvErrors, AgvErrorStatusReceived errorStatusReceived)
    {
        if (agvErrors == null || !agvErrors.Any())
        {
            errorStatusReceived.ShouldBeNull();
            return;
        }

        foreach (var actualAgvError in agvErrors)
        {
            actualAgvError.AgvCorrelationId.ShouldBe(errorStatusReceived.CorrelationId);
            var expectedAgvError = errorStatusReceived.Errors!.Find(e => e.ErrorType == actualAgvError.ErrorType);
            expectedAgvError.ShouldNotBeNull();
            actualAgvError.ErrorDescription.ShouldBe(expectedAgvError.ErrorDescription);
            actualAgvError.ErrorLevel.ShouldBe(expectedAgvError.ErrorLevel);
            actualAgvError.ErrorType.ShouldBe(expectedAgvError.ErrorType);
            actualAgvError.ErrorReferences.ShouldBe(expectedAgvError.ErrorReferences);
        }
    }

    [TestMethod]
    public async Task UpdateAgvErrorStatusAsync_WhenSuccessfullyAddedToRegistry_CanGetAgvError()
    {
        const int errorCount = 5;
        var agvCorrelationId = Guid.NewGuid();
        var agvErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(errorCount, agvCorrelationId: agvCorrelationId);
        var fixture = new AgvErrorRegistryTestFixture();

        await fixture.AgvErrorRegistry.UpdateAgvErrorStatusAsync(agvErrorStatus.Errors, agvErrorStatus.CorrelationId);
        IList<AgvError> agvErrorsInRegistry = await fixture.AgvErrorRegistry.GetActiveErrorsAsync(agvCorrelationId);

        agvErrorsInRegistry.ShouldNotBeNull();
        VerifyAgvErrorsInRegistry(agvErrorsInRegistry, agvErrorStatus);
    }

    [TestMethod]
    public async Task UpdateAgvErrorStatusAsync_WhenErrorTypeNotIncludedInAgvErrorStatus_ResolveAgvError()
    {
        const int errorCount = 5;
        var agvCorrelationId = Guid.NewGuid();
        var agvErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(errorCount, agvCorrelationId: agvCorrelationId);
        var fixture = await new AgvErrorRegistryTestFixture()
            .WithAgvErrorStatus(agvErrorStatus)
            .BuildAsync();
        IList<AgvError> agvErrorsInRegistry = await fixture.AgvErrorRegistry.GetActiveErrorsAsync(agvCorrelationId);
        foreach (var agvError in agvErrorsInRegistry)
        {
            agvError.ErrorResolved.ShouldBeNull();
        }

        agvErrorStatus.Errors = null;
        await fixture.AgvErrorRegistry.UpdateAgvErrorStatusAsync(agvErrorStatus.Errors, agvErrorStatus.CorrelationId);

        agvErrorsInRegistry = await fixture.AgvErrorRegistry.GetActiveErrorsAsync(agvCorrelationId);
        //all errors resolved
        agvErrorsInRegistry.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorsAsync_WhenThereAreNoActiveErrorsForAgv_ReturnsEmptyAgvErrors()
    {
        var fixture = new AgvErrorRegistryTestFixture();

        IList<AgvError> actualAgvErrors = await fixture.AgvErrorRegistry.GetActiveErrorsAsync(Guid.NewGuid());

        actualAgvErrors.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorsAsync_WhenThereAreActiveErrorsForAgv_ReturnsAgvErrors()
    {
        const int errorCount = 5;
        var agvCorrelationId = Guid.NewGuid();
        var agvErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(errorCount, agvCorrelationId: agvCorrelationId);
        var fixture = await new AgvErrorRegistryTestFixture()
            .WithAgvErrorStatus(agvErrorStatus)
            .BuildAsync();

        IList<AgvError> actualAgvErrors = await fixture.AgvErrorRegistry.GetActiveErrorsAsync(agvCorrelationId);

        actualAgvErrors.ShouldNotBeEmpty();
        VerifyAgvErrorsInRegistry(actualAgvErrors, agvErrorStatus);
    }

    [TestMethod]
    public async Task GetErrorHistoryAsync_WhenThereAreNoErrorsForAgv_ReturnsEmptyAgvErrors()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvErrorRegistryTestFixture();

        IReadOnlyList<AgvError> actualAgvErrors = await fixture.AgvErrorRegistry.GetErrorsAsync(agvCorrelationId, DateTime.MinValue, DateTime.MaxValue);

        actualAgvErrors.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorHistoryAsync_WhenThereAreErrorsForAgv_ReturnsAgvErrors()
    {
        const int errorCount = 5;
        var agvCorrelationId = Guid.NewGuid();
        var agvErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(errorCount, agvCorrelationId: agvCorrelationId);
        var fixture = await new AgvErrorRegistryTestFixture()
            .WithAgvErrorStatus(agvErrorStatus)
            .BuildAsync();

        IReadOnlyList<AgvError> actualAgvErrors = await fixture.AgvErrorRegistry.GetErrorsAsync(agvCorrelationId, DateTime.MinValue, DateTime.MaxValue);

        actualAgvErrors.ShouldNotBeEmpty();
        VerifyAgvErrorsInRegistry((IList<AgvError>?) actualAgvErrors, agvErrorStatus);
    }
}