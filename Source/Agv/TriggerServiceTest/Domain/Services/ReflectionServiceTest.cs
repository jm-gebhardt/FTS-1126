using Gebhardt.Acc.TriggerService.Test.Fixtures;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TriggerService.Test.Domain.Services;

[TestClass]
public class ReflectionServiceTest
{
    private interface ITestInterface;

    // ReSharper disable once ClassCannotBeInstantiated
    private class WithoutConstructor : ITestInterface
    {
        private WithoutConstructor(){}
    }

    private class WithEmptyConstructor : ITestInterface;

#pragma warning disable CS9113 // Parameter is unread.
    private class WithInjectedDependency(IBus bus) : ITestInterface;

    public class WithProvidedDependency(ILogger<WithProvidedDependency> logger) : ITestInterface;

    private class WithUnknownDependency(WithEmptyConstructor unknownDependency) : ITestInterface;
#pragma warning restore CS9113 // Parameter is unread.

    [TestMethod]
    public void GetInstance_NoPublicConstructors_ReturnsNull()
    {
        var fixture = new ReflectionServiceTestFixture();

        var reflectedType = typeof(WithoutConstructor);

        fixture.ReflectionService.GetInstance<ITestInterface>(reflectedType, []).ShouldBeNull();
    }

    [TestMethod]
    public void GetInstance_WithEmptyConstructor_ReturnsInstance()
    {
        var fixture = new ReflectionServiceTestFixture();

        var reflectedType = typeof(WithEmptyConstructor);
        fixture.ReflectionService.GetInstance<ITestInterface>(reflectedType, []).ShouldBeEquivalentTo(new WithEmptyConstructor());
    }

    [TestMethod]
    public void GetInstance_WithInjectedDependency_ReturnsInstance()
    {
        var fixture = new ReflectionServiceTestFixture();

        var reflectedType = typeof(WithInjectedDependency);
        fixture.ReflectionService.GetInstance<ITestInterface>(reflectedType, []).ShouldNotBeNull();
    }

    [TestMethod]
    public void GetInstance_WithProvidedDependency_ReturnsInstance()
    {
        var fixture = new ReflectionServiceTestFixture();

        var reflectedType = typeof(WithProvidedDependency);
        ILogger<WithProvidedDependency> dependency = new Mock<ILogger<WithProvidedDependency>>().Object;
        fixture.ReflectionService.GetInstance<ITestInterface>(reflectedType, [dependency]).ShouldBeEquivalentTo(new WithProvidedDependency(dependency));
    }

    [TestMethod]
    public void GetInstance_WithUnknownDependency_ReturnsNull()
    {
        var fixture = new ReflectionServiceTestFixture();

        var reflectedType = typeof(WithUnknownDependency);
        fixture.ReflectionService.GetInstance<ITestInterface>(reflectedType, []).ShouldBeNull();
    }
}