using FluentValidation;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Validators;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.RequestResponseHandler;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services.RequestResponseHandler;

public class SimulatedAgvTypeConfigurationRequestHandlerTestFixture
    : MediatorTestFixture<RequestAddAgvTypeConfiguration, SimulatedAgvTypeConfigurationRequestHandler, MockConsumer>
{
    private const string Manufacturer = SimulatedAgvTypeConfigurationGenerator.Manufacturer;

    public const string AgvTypeIdentifier = SimulatedAgvTypeConfigurationGenerator.AgvTypeIdentifier;
    public const string OtherAgvTypeIdentifier = SimulatedAgvTypeConfigurationGenerator.OtherAgvTypeIdentifier;
    public const string IdentifierWithoutSeparator = Manufacturer;
    public const int NoAgvs = 0;
    public const int NoOfAgvs = 3;
    public const int UpdatedNoOfAgvs = 5;
    public const int NegativeNoOfAgvs = -1;

    private readonly List<SimulatedAgvTypeConfiguration> _storedConfigurations = [];
    private readonly Mock<ISimulatedAgvTypeConfigurationRepository> _repository = new();
    private readonly Mock<ISimulatedAgvSpawner> _spawner = new();

    public SimulatedAgvTypeConfiguration? AddedConfiguration { get; private set; }

    public SimulatedAgvTypeConfiguration? UpdatedConfiguration { get; private set; }

    public Guid? DeletedAgvTypeId { get; private set; }

    public bool FleetApplied { get; private set; }

    public SimulatedAgvTypeConfiguration? AppliedCurrentConfiguration { get; private set; }

    public SimulatedAgvTypeConfiguration? AppliedNewConfiguration { get; private set; }

    public SimulatedAgvTypeConfigurationRequestHandlerTestFixture()
    {
        _repository.Setup(repository => repository.GetAsync(It.IsAny<Guid>())).ReturnsAsync((SimulatedAgvTypeConfiguration?)null);
        _repository.Setup(repository => repository.GetAllAsync()).ReturnsAsync(_storedConfigurations);
        _repository.Setup(repository => repository.AddAsync(It.IsAny<SimulatedAgvTypeConfiguration>()))
            .Callback((SimulatedAgvTypeConfiguration configuration) => AddedConfiguration = configuration)
            .Returns(Task.CompletedTask);
        _repository.Setup(repository => repository.UpdateAsync(It.IsAny<SimulatedAgvTypeConfiguration>()))
            .Callback((SimulatedAgvTypeConfiguration configuration) => UpdatedConfiguration = configuration)
            .Returns(Task.CompletedTask);
        _repository.Setup(repository => repository.DeleteAsync(It.IsAny<Guid>()))
            .Callback((Guid agvTypeId) => DeletedAgvTypeId = agvTypeId)
            .Returns(Task.CompletedTask);
        _spawner.Setup(spawner => spawner.ApplyAsync(It.IsAny<SimulatedAgvTypeConfiguration>(),
                It.IsAny<SimulatedAgvTypeConfiguration>(),
                It.IsAny<CancellationToken>()))
            .Callback((SimulatedAgvTypeConfiguration? currentConfiguration, SimulatedAgvTypeConfiguration? newConfiguration, CancellationToken _) =>
            {
                FleetApplied = true;
                AppliedCurrentConfiguration = currentConfiguration;
                AppliedNewConfiguration = newConfiguration;
            })
            .Returns(Task.CompletedTask);
    }

    public SimulatedAgvTypeConfigurationRequestHandlerTestFixture WithStoredConfiguration(SimulatedAgvTypeConfiguration configuration)
    {
        _storedConfigurations.Add(configuration);
        _repository.Setup(repository => repository.GetAsync(configuration.Id)).ReturnsAsync(configuration);
        return this;
    }

    public new SimulatedAgvTypeConfigurationRequestHandlerTestFixture Build()
    {
        AddServices(services =>
        {
            services.AddSingleton<IValidator<SimulatedAgvTypeConfiguration>, SimulatedAgvTypeConfigurationValidator>();
            services.AddSingleton(_repository.Object);
            services.AddSingleton(_spawner.Object);
        });
        ConfigureAdditionalRequestClient<SimulatedAgvTypeConfigurationRequestHandler, RequestUpdateAgvTypeConfiguration>();
        ConfigureAdditionalRequestClient<SimulatedAgvTypeConfigurationRequestHandler, RequestDeleteAgvTypeConfiguration>();
        ConfigureAdditionalRequestClient<SimulatedAgvTypeConfigurationRequestHandler, RequestAgvTypeConfiguration>();
        ConfigureAdditionalRequestClient<SimulatedAgvTypeConfigurationRequestHandler, RequestAgvTypeConfigurations>();
        base.Build();
        return this;
    }
}