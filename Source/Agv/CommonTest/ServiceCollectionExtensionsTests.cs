using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.Acc.CommonTest;

/// <summary>
/// Tests for ServiceCollectionExtensions
/// </summary>
[TestClass]
public class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Test no RabbitMQ config provided
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void ConfigureServices_NoConfigProvided()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>()
        ).Build();
        ServiceCollection services = new();
        services.ConfigureServicesMassTransit(configuration);
    }

    /// <summary>
    /// Test valid RabbitMQ config provided
    /// </summary>
    [TestMethod]
    public void ConfigureServices_ValidConfigProvided()
    {
        string sectionName = new ConfigurationTypeManager().GetSectionName<MassTransitConfiguration>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { { $"{sectionName}:Url", "127.0.0.1" } }
        ).Build();
        ServiceCollection services = new();

        services.ConfigureServicesMassTransit(configuration);
    }
}