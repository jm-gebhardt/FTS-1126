using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class AgvErrorRepositoryTest
{
    private static void VerifyAgvErrorsInRepository(IReadOnlyList<AgvError> agvErrors, AgvErrorStatusReceived errorStatusReceived)
    {
        if (!agvErrors.Any())
        {
            errorStatusReceived.ShouldBeNull();
            return;
        }

        foreach (var actualAgvError in agvErrors)
        {
            actualAgvError.AgvCorrelationId.ShouldBe(errorStatusReceived.CorrelationId);
            var expectedAgvError = errorStatusReceived.Errors.Find(e => e.ErrorType == actualAgvError.ErrorType);
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

        await fixture.AgvErrorRepository.AddAndUpdateAgvErrorAsync(agvErrorStatus.Errors, [], agvErrorStatus.CorrelationId);
        var agvErrorsInRepository = await fixture.AgvErrorRepository.GetActiveErrorsAsync(agvCorrelationId);

        agvErrorsInRepository.ShouldNotBeNull();
        VerifyAgvErrorsInRepository(agvErrorsInRepository, agvErrorStatus);
        VerifyAgvErrorsInRepository(fixture.GetCachedErrors(agvCorrelationId), agvErrorStatus);
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
        var agvErrorsInRepository = await fixture.AgvErrorRepository.GetActiveErrorsAsync(agvCorrelationId);
        agvErrorsInRepository.ShouldAllBe(x => x.ErrorResolved == null);

        // simulate equal, but no reference objects
        var errorsToResolve = agvErrorsInRepository.DeepCopy().ToList();
        await fixture.AgvErrorRepository.AddAndUpdateAgvErrorAsync([], errorsToResolve, agvErrorStatus.CorrelationId);

        agvErrorsInRepository = await fixture.AgvErrorRepository.GetActiveErrorsAsync(agvCorrelationId);

        agvErrorsInRepository.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task GetActiveErrorsAsync_WhenThereAreNoActiveErrorsForAgv_ReturnsEmptyAgvErrors()
    {
        var fixture = new AgvErrorRepositoryTestFixture();

        var actualAgvErrors = await fixture.AgvErrorRepository.GetActiveErrorsAsync(Guid.NewGuid());

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

        var actualAgvErrors = await fixture.AgvErrorRepository.GetActiveErrorsAsync(agvCorrelationId);

        actualAgvErrors.ShouldNotBeEmpty();
        VerifyAgvErrorsInRepository(actualAgvErrors, agvErrorStatus);
        VerifyAgvErrorsInRepository(fixture.GetCachedErrors(agvCorrelationId), agvErrorStatus);
    }

    [TestMethod]
    public async Task GetErrorsAsync_WhenThereAreNoErrorsForAgv_ReturnsEmptyAgvErrors()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvErrorRepositoryTestFixture();

        var actualAgvErrors = await fixture.AgvErrorRepository.GetErrorsAsync(agvCorrelationId, DateTime.MinValue, DateTime.MaxValue);

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

        var actualAgvErrors = await fixture.AgvErrorRepository.GetErrorsAsync(agvCorrelationId, DateTime.MinValue, DateTime.MaxValue);

        actualAgvErrors.ShouldNotBeEmpty();
        VerifyAgvErrorsInRepository(actualAgvErrors.ToList(), agvErrorStatus);
        VerifyAgvErrorsInRepository(fixture.GetCachedErrors(agvCorrelationId), agvErrorStatus);
    }

    [TestMethod]
    public async Task LoadInMemoryCache_OnlyUnresolvedErrorsLoaded()
    {
        const int unresolvedCount = 2;
        const int resolvedCount = 3;
        var agvCorrelationId = Guid.NewGuid();
        var unresolvedStatus = AgvErrorGenerator.GenerateAgvErrorStatus(unresolvedCount, agvCorrelationId);
        var resolvedErrors = AgvErrorGenerator.GenerateAgvErrorStatus(resolvedCount, agvCorrelationId).Errors
            .Select(e =>
            {
                e.ErrorResolved = DateTime.UtcNow.AddMinutes(-5);
                return e;
            }).ToList();

        var fixture = await new AgvErrorRepositoryTestFixture()
            .WithAgvErrorStatus(unresolvedStatus)
            .WithAgvErrors(resolvedErrors)
            .BuildAsync();

        await fixture.AgvErrorRepository.LoadInMemoryCache();

        var cached = fixture.GetCachedErrors(agvCorrelationId);
        cached.Count.ShouldBe(unresolvedCount);
        cached.ShouldAllBe(e => e.ErrorResolved == null);
    }

    [TestMethod]
    public async Task GetActiveErrorsAsync_OnlyReturnsDataFromCache()
    {
        var agv1 = Guid.NewGuid();
        var agv2 = Guid.NewGuid();
        var errorAgv1 = AgvErrorGenerator.GenerateAgvError(agv1);
        var errorAgv2 = AgvErrorGenerator.GenerateAgvError(agv2);

        var fixture = await new AgvErrorRepositoryTestFixture()
            .WithAgvErrors([errorAgv1])
            .BuildAsync();

        fixture.Add(errorAgv2);

        fixture.GetCachedErrors(agv1).Count.ShouldBe(1);
        fixture.GetCachedErrors(agv2).ShouldBeEmpty();

        var result = await fixture.AgvErrorRepository.GetActiveErrorsAsync(new List<Guid> { agv1, agv2 });

        result.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetErrorsAsync_IgnoresCache_ReturnsOnlyDatabaseResults()
    {
        var agvInDb = Guid.NewGuid();
        var errorOnlyInDb = AgvErrorGenerator.GenerateAgvError(agvInDb);

        var fixture = await new AgvErrorRepositoryTestFixture()
            .BuildAsync();

        fixture.Add(errorOnlyInDb);

        var result = await fixture.AgvErrorRepository.GetErrorsAsync([agvInDb], DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));

        result.Count.ShouldBe(1);
        result.Single().AgvCorrelationId.ShouldBe(agvInDb);
    }

    [TestMethod]
    public async Task AddOrUpdateAgvErrorStatus_WhenErrorReappearsAfterResolution_AddsAgain()
    {
        var agv = Guid.NewGuid();
        var error = AgvErrorGenerator.GenerateAgvError(agv);
        var fixture = await new AgvErrorRepositoryTestFixture().BuildAsync();

        await fixture.AgvErrorRepository.AddAndUpdateAgvErrorAsync([error], [], agv);
        await fixture.AgvErrorRepository.AddAndUpdateAgvErrorAsync([], [error], agv);
        var activeErrors = await fixture.AgvErrorRepository.GetActiveErrorsAsync(agv);
        activeErrors.ShouldBeEmpty();
        error = AgvErrorGenerator.GenerateAgvError(agv);

        await fixture.AgvErrorRepository.AddAndUpdateAgvErrorAsync([error], [], agv);

        activeErrors = await fixture.AgvErrorRepository.GetActiveErrorsAsync(agv);
        activeErrors.Count.ShouldBe(1);
        activeErrors.Single().CorrelationId.ShouldBe(error.CorrelationId);
    }
}