using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

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

        await fixture.AgvErrorRepository.AddOrUpdateAgvErrorStatus(agvErrorStatus.Errors, agvErrorStatus.CorrelationId);
        IList<AgvError> agvErrorsInRepository = await fixture.AgvErrorRepository.GetActiveErrorsAsync(agvCorrelationId);

        agvErrorsInRepository.ShouldNotBeNull();
        VerifyAgvErrorsInRepository(agvErrorsInRepository, agvErrorStatus);
        VerifyAgvErrorsInRepository(fixture.GetCachedErrors(agvCorrelationId), agvErrorStatus);
    }

    [TestMethod]
    public async Task AddOrUpdateAgvErrorStatus_WhenErrorBecomesAWarning_ReturnsTrue()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvError = AgvErrorGenerator.GenerateAgvError(agvCorrelationId: agvCorrelationId, errorLevel: ErrorLevel.Fatal);
        var fixture = new AgvErrorRepositoryTestFixture();

        await fixture.AgvErrorRepository.AddOrUpdateAgvErrorStatus([agvError], agvCorrelationId);
        agvError = AgvErrorGenerator.GenerateAgvError(agvCorrelationId: agvCorrelationId, errorLevel: ErrorLevel.Warning);

        (bool changed, List<AgvError> newErrors, List<AgvError> resolvedErrors) = await fixture.AgvErrorRepository.AddOrUpdateAgvErrorStatus([agvError], agvCorrelationId);
        changed.ShouldBeTrue();
        newErrors.Single().ShouldBe(agvError);
        resolvedErrors.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task AddOrUpdateAgvErrorStatus_WhenErrorTypeNotIncludedInAgvErrorStatus_ResolveAgvError()
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
        (bool changed, List<AgvError> newErrors, List<AgvError> resolvedErrors) = await fixture.AgvErrorRepository.AddOrUpdateAgvErrorStatus(agvErrorStatus.Errors, agvErrorStatus.CorrelationId);

        agvErrorsInRepository = await fixture.AgvErrorRepository.GetActiveErrorsAsync(agvCorrelationId);

        changed.ShouldBeTrue();
        newErrors.ShouldBeEmpty();
        resolvedErrors.Count.ShouldBe(errorCount);
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
        VerifyAgvErrorsInRepository(fixture.GetCachedErrors(agvCorrelationId), agvErrorStatus);
    }

    [TestMethod]
    public async Task GetErrorsAsync_WhenThereAreNoErrorsForAgv_ReturnsEmptyAgvErrors()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvErrorRepositoryTestFixture();

        IReadOnlyList<AgvError> actualAgvErrors = await fixture.AgvErrorRepository.GetErrorsAsync(agvCorrelationId, DateTime.MinValue, DateTime.MaxValue);

        actualAgvErrors.ShouldBeEmpty();
        fixture.GetCachedErrors(agvCorrelationId).ShouldBeEmpty();
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
        VerifyAgvErrorsInRepository(fixture.GetCachedErrors(agvCorrelationId), agvErrorStatus);
    }
}