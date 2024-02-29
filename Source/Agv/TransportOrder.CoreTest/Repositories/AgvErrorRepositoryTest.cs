using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

[TestClass]
public class AgvErrorRepositoryTest
{
    private static void VerifyAgvErrorsInRepository(IList<AgvError>? agvErrors, AgvErrorStatusReceived errorStatusReceived)
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
    public async Task GetActiveErrorsAsync_WhenSuccessfullyAddedToRepository_CanGetAgvError()
    {
        const int errorCount = 5;
        var agvCorrelationId = Guid.NewGuid();
        var agvErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(errorCount, agvCorrelationId);
        var fixture = new AgvErrorRepositoryTestFixture();

        await fixture.AgvErrorRepository.AddOrUpdateAgvErrorStatusAsync(agvErrorStatus.Errors, agvErrorStatus.CorrelationId);
        IList<AgvError> agvErrorsInRepository = await fixture.AgvErrorRepository.GetActiveErrorsAsync(agvCorrelationId);

        agvErrorsInRepository.ShouldNotBeNull();
        VerifyAgvErrorsInRepository(agvErrorsInRepository, agvErrorStatus);
    }

    [TestMethod]
    public async Task AddOrUpdateAgvErrorStatusAsync_WhenErrorBecomesAWarning_ReturnsTrue()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvError = AgvErrorGenerator.GenerateAgvError(agvCorrelationId: agvCorrelationId, errorLevel: ErrorLevel.Fatal);
        var fixture = new AgvErrorRepositoryTestFixture();

        await fixture.AgvErrorRepository.AddOrUpdateAgvErrorStatusAsync([agvError], agvCorrelationId);  
        agvError = AgvErrorGenerator.GenerateAgvError(agvCorrelationId: agvCorrelationId, errorLevel: ErrorLevel.Warning);

        bool changed = await fixture.AgvErrorRepository.AddOrUpdateAgvErrorStatusAsync([agvError], agvCorrelationId);
        changed.ShouldBeTrue();
    }

    [TestMethod]
    public async Task AddOrUpdateAgvErrorStatusAsync_WhenErrorTypeNotIncludedInAgvErrorStatus_ResolveAgvError()
    {
        const int errorCount = 5;
        var agvCorrelationId = Guid.NewGuid();
        var agvErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(errorCount, agvCorrelationId);
        var fixture = await new AgvErrorRepositoryTestFixture()
            .WithAgvErrorStatus(agvErrorStatus)
            .BuildAsync();
        IList<AgvError> agvErrorsInRepository = await fixture.AgvErrorRepository.GetActiveErrorsAsync(agvCorrelationId);
        foreach (var agvError in agvErrorsInRepository)
        {
            agvError.ErrorResolved.ShouldBeNull();
        }

        agvErrorStatus.Errors = null;
        await fixture.AgvErrorRepository.AddOrUpdateAgvErrorStatusAsync(agvErrorStatus.Errors, agvErrorStatus.CorrelationId);

        agvErrorsInRepository = await fixture.AgvErrorRepository.GetActiveErrorsAsync(agvCorrelationId);
        //all errors resolved
        agvErrorsInRepository.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetActiveErrorsAsync_WhenThereAreNoActiveErrorsForAgv_ReturnsEmptyAgvErrors()
    {
        var fixture = new AgvErrorRepositoryTestFixture();

        IList<AgvError> actualAgvErrors = await fixture.AgvErrorRepository.GetActiveErrorsAsync(Guid.NewGuid());

        actualAgvErrors.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetActiveErrorsAsync_WhenThereAreActiveErrorsForAgv_ReturnsAgvErrors()
    {
        const int errorCount = 5;
        var agvCorrelationId = Guid.NewGuid();
        var agvErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(errorCount, agvCorrelationId);
        var fixture = await new AgvErrorRepositoryTestFixture()
            .WithAgvErrorStatus(agvErrorStatus)
            .BuildAsync();

        IList<AgvError> actualAgvErrors = await fixture.AgvErrorRepository.GetActiveErrorsAsync(agvCorrelationId);

        actualAgvErrors.ShouldNotBeEmpty();
        VerifyAgvErrorsInRepository(actualAgvErrors, agvErrorStatus);
    }

    [TestMethod]
    public async Task GetErrorsAsync_WhenThereAreNoErrorsForAgv_ReturnsEmptyAgvErrors()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvErrorRepositoryTestFixture();

        IReadOnlyList<AgvError> actualAgvErrors = await fixture.AgvErrorRepository.GetErrorsAsync(agvCorrelationId, DateTime.MinValue, DateTime.MaxValue);

        actualAgvErrors.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorsAsync_WhenThereAreErrorsForAgv_ReturnsAgvErrors()
    {
        const int errorCount = 5;
        var agvCorrelationId = Guid.NewGuid();
        var agvErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(errorCount, agvCorrelationId);
        var fixture = await new AgvErrorRepositoryTestFixture()
            .WithAgvErrorStatus(agvErrorStatus)
            .BuildAsync();

        IReadOnlyList<AgvError> actualAgvErrors = await fixture.AgvErrorRepository.GetErrorsAsync(agvCorrelationId, DateTime.MinValue, DateTime.MaxValue);

        actualAgvErrors.ShouldNotBeEmpty();
        VerifyAgvErrorsInRepository((IList<AgvError>?) actualAgvErrors, agvErrorStatus);
    }
}