using FluentValidation;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ValidationException = Gebhardt.Acc.Common.Domain.Exceptions.ValidationException;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.RequestResponseHandler;

/// <summary>
/// Handles requests for the simulation configuration of agv types.
/// </summary>
[UsedImplicitly]
public class SimulatedAgvTypeConfigurationRequestHandler(
    ILogger<SimulatedAgvTypeConfigurationRequestHandler> logger,
    IValidator<SimulatedAgvTypeConfiguration> validator,
    ISimulatedAgvTypeConfigurationRepository repository,
    ISimulatedAgvSpawner spawner) :
    IConsumer<RequestAddAgvTypeConfiguration>,
    IConsumer<RequestUpdateAgvTypeConfiguration>,
    IConsumer<RequestDeleteAgvTypeConfiguration>,
    IConsumer<RequestAgvTypeConfiguration>,
    IConsumer<RequestAgvTypeConfigurations>
{
    private async Task ValidateAsync(SimulatedAgvTypeConfiguration configuration, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(configuration, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(string.Join(" ", validationResult.Errors.Select(failure => failure.ErrorMessage)));
        }
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestAddAgvTypeConfiguration> context)
    {
        (_, string agvTypeIdentifier, var settings) = context.Message;
        var configuration = new SimulatedAgvTypeConfiguration(agvTypeIdentifier).ApplySettings(settings);
        await ValidateAsync(configuration, context.CancellationToken);
        if (await repository.GetAsync(configuration.Id) != null)
        {
            throw new AgvTypeConfigurationAlreadyExistsException(agvTypeIdentifier);
        }

        logger.LogInformation("Adding the simulation configuration of agv type {AgvTypeIdentifier} with {NoOfAgvs} agvs", agvTypeIdentifier, configuration.NoOfAgvs);
        await repository.AddAsync(configuration);
        await spawner.ApplyAsync(null, configuration, context.CancellationToken);
        await context.RespondAsync(new AgvTypeConfigurationResponse(configuration));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestUpdateAgvTypeConfiguration> context)
    {
        var (agvTypeId, settings) = context.Message;
        var currentConfiguration = await repository.GetRequiredAsync(agvTypeId);
        var newConfiguration = currentConfiguration.ApplySettings(settings);
        await ValidateAsync(newConfiguration, context.CancellationToken);

        logger.LogInformation("Updating the simulation configuration of agv type {AgvTypeIdentifier} to {NoOfAgvs} agvs", newConfiguration.AgvTypeIdentifier, newConfiguration.NoOfAgvs);
        await repository.UpdateAsync(newConfiguration);
        await spawner.ApplyAsync(currentConfiguration, newConfiguration, context.CancellationToken);
        await context.RespondAsync(new AgvTypeConfigurationResponse(newConfiguration));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestDeleteAgvTypeConfiguration> context)
    {
        var agvTypeId = context.Message.CorrelationId;
        var currentConfiguration = await repository.GetRequiredAsync(agvTypeId);

        logger.LogInformation("Deleting the simulation configuration of agv type {AgvTypeIdentifier}", currentConfiguration.AgvTypeIdentifier);
        await repository.DeleteAsync(agvTypeId);
        await spawner.ApplyAsync(currentConfiguration, null, context.CancellationToken);
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestAgvTypeConfiguration> context)
    {
        var configuration = await repository.GetRequiredAsync(context.Message.CorrelationId);
        await context.RespondAsync(new AgvTypeConfigurationResponse(configuration));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestAgvTypeConfigurations> context)
    {
        IReadOnlyCollection<SimulatedAgvTypeConfiguration> configurations = await repository.GetAllAsync();
        await context.RespondAsync(new AgvTypeConfigurationsResponse(configurations));
    }
}