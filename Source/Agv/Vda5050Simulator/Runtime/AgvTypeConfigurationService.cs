using Gebhardt.Acc.Vda5050Simulator.Domain;
using Gebhardt.Acc.Vda5050Simulator.Repositories;
using Gebhardt.Acc.Vda5050Simulator.WebApi.Dtos;
using ValidationException = Gebhardt.Acc.Common.Domain.Exceptions.ValidationException;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

/// <summary>
/// Validates and persists agv type configurations and brings the fleet in line with them.
/// </summary>
public class AgvTypeConfigurationService(
    ILogger<AgvTypeConfigurationService> logger,
    IAgvTypeConfigurationRepository repository,
    FleetHost fleet)
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static void Validate(AgvTypeConfiguration configuration)
    {
        var errors = configuration.Validate();
        if (errors.Count > 0)
        {
            throw new ValidationException(string.Join(" ", errors));
        }
    }

    private static AgvTypeConfiguration Apply(AgvTypeConfiguration configuration, UpdateAgvTypeConfigurationDto settings)
    {
        return configuration with
        {
            NoOfAgvs = settings.NoOfAgvs ?? configuration.NoOfAgvs,
            StateUpdateIntervalMs = settings.StateUpdateIntervalMs ?? configuration.StateUpdateIntervalMs,
            VisualizationUpdateIntervalMs = settings.VisualizationUpdateIntervalMs ?? configuration.VisualizationUpdateIntervalMs,
            BatteryDischargeRate = settings.BatteryDischargeRate ?? configuration.BatteryDischargeRate,
            BatteryChargeRate = settings.BatteryChargeRate ?? configuration.BatteryChargeRate,
            InitialBatteryCharge = settings.InitialBatteryCharge ?? configuration.InitialBatteryCharge,
            MaxSpeed = settings.MaxSpeed ?? configuration.MaxSpeed
        };
    }

    private async Task ApplyToFleetAsync(AgvTypeConfiguration? current, AgvTypeConfiguration? updated)
    {
        var currentSerials = current?.SerialNumbers().ToHashSet() ?? [];
        var newSerials = updated?.SerialNumbers().ToHashSet() ?? [];
        if (updated is not null)
        {
            foreach (string serial in newSerials.Except(currentSerials))
            {
                fleet.AddAgv(updated.ToAgvConfiguration(serial));
            }
        }
        if (current is not null)
        {
            foreach (string serial in currentSerials.Except(newSerials))
            {
                await fleet.RemoveAgvAsync(new AgvIdentity(current.Manufacturer, current.SeriesName, serial));
            }
        }
    }

    public async Task<AgvTypeConfiguration> AddAsync(AddAgvTypeConfigurationDto dto)
    {
        await _lock.WaitAsync();
        try
        {
            var configuration = Apply(new AgvTypeConfiguration(dto.AgvTypeIdentifier), dto);
            Validate(configuration);
            if (await repository.GetAsync(configuration.Id) != null)
            {
                throw new AgvTypeConfigurationAlreadyExistsException(configuration.AgvTypeIdentifier);
            }
            logger.LogInformation("Adding the simulation configuration of agv type {AgvTypeIdentifier} with {NoOfAgvs} agvs", configuration.AgvTypeIdentifier, configuration.NoOfAgvs);
            await repository.AddAsync(configuration);
            await ApplyToFleetAsync(null, configuration);
            return configuration;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Stores the configuration unless the agv type is already configured.
    /// </summary>
    /// <returns>True if the configuration was added, false if it already existed.</returns>
    public async Task<bool> EnsureAsync(string agvTypeIdentifier, AddAgvTypeConfigurationDto settings)
    {
        await _lock.WaitAsync();
        try
        {
            var configuration = Apply(new AgvTypeConfiguration(agvTypeIdentifier), settings);
            if (await repository.GetAsync(configuration.Id) != null)
            {
                logger.LogInformation("The agv type {AgvTypeIdentifier} is already configured, skipping", agvTypeIdentifier);
                return false;
            }
            Validate(configuration);
            logger.LogInformation("Adding the simulation configuration of agv type {AgvTypeIdentifier} with {NoOfAgvs} agvs", configuration.AgvTypeIdentifier, configuration.NoOfAgvs);
            await repository.AddAsync(configuration);
            await ApplyToFleetAsync(null, configuration);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<AgvTypeConfiguration> UpdateAsync(Guid agvTypeId, UpdateAgvTypeConfigurationDto dto)
    {
        await _lock.WaitAsync();
        try
        {
            var current = await GetRequiredAsync(agvTypeId);
            var updated = Apply(current, dto);
            Validate(updated);
            logger.LogInformation("Updating the simulation configuration of agv type {AgvTypeIdentifier} to {NoOfAgvs} agvs", updated.AgvTypeIdentifier, updated.NoOfAgvs);
            await repository.UpdateAsync(updated);
            await ApplyToFleetAsync(current, updated);
            return updated;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(Guid agvTypeId)
    {
        await _lock.WaitAsync();
        try
        {
            var current = await GetRequiredAsync(agvTypeId);
            logger.LogInformation("Deleting the simulation configuration of agv type {AgvTypeIdentifier}", current.AgvTypeIdentifier);
            await repository.DeleteAsync(agvTypeId);
            await ApplyToFleetAsync(current, null);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<AgvTypeConfiguration> GetRequiredAsync(Guid agvTypeId)
    {
        return await repository.GetAsync(agvTypeId) ?? throw new AgvTypeConfigurationNotFoundException(agvTypeId);
    }

    public Task<IReadOnlyCollection<AgvTypeConfiguration>> GetAllAsync()
    {
        return repository.GetAllAsync();
    }

    /// <summary>
    /// Spawns the agvs of all persisted configurations.
    /// </summary>
    public async Task SpawnPersistedAsync()
    {
        foreach (var configuration in await repository.GetAllAsync())
        {
            await ApplyToFleetAsync(null, configuration);
        }
    }
}