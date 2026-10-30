using DeviceCommunication.Vda5050.Simulator.Domain;
using DeviceCommunication.Vda5050.Simulator.Runtime;
using DeviceCommunication.Vda5050.Simulator.WebApi.Controllers;
using DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DeviceCommunication.Vda5050.Simulator.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for managing the simulation configuration of agv types.
/// </summary>
public static class AgvTypeConfigurationEndpoints
{
    private const string LoggerCategory = "Gebhardt.Acc.Vda5050Simulator.WebApi.Endpoints.AgvTypeConfiguration";

    /// <summary>
    /// Maps the agv type configuration endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapAgvTypeConfigurationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(string.Empty)
            .RequireAuthorization()
            .WithGroupName(RestApiGroup.GroupName)
            .WithTags("AgvTypeConfiguration");

        group.MapPost("agvTypeConfiguration", AddAsync)
            .WithName("AddAgvTypeConfiguration")
            .WithSummary("Add the simulation configuration of an agv type.");
        group.MapPatch("agvTypeConfiguration/{agvTypeId:guid}", UpdateAsync)
            .WithName("UpdateAgvTypeConfiguration")
            .WithSummary("Change the simulation configuration of an agv type.");
        group.MapDelete("agvTypeConfiguration/{agvTypeId:guid}", DeleteAsync)
            .WithName("DeleteAgvTypeConfiguration")
            .WithSummary("Delete the simulation configuration of an agv type and despawn its agvs.");
        group.MapGet("agvTypeConfiguration/{agvTypeId:guid}", GetAsync)
            .WithName("GetAgvTypeConfiguration")
            .WithSummary("Get the simulation configuration of an agv type.");
        group.MapGet("agvTypeConfigurations", GetAllAsync)
            .WithName("GetAgvTypeConfigurations")
            .WithSummary("Get the simulation configurations of all agv types.");

        return routes;
    }

    private static AgvTypeConfigurationDto ToDto(AgvTypeConfiguration configuration)
    {
        return new AgvTypeConfigurationDto
        {
            AgvTypeId = configuration.Id,
            AgvTypeIdentifier = configuration.AgvTypeIdentifier,
            Manufacturer = configuration.Manufacturer,
            SeriesName = configuration.SeriesName,
            SerialNumbers = configuration.SerialNumbers().ToList(),
            NoOfAgvs = configuration.NoOfAgvs,
            StateUpdateIntervalMs = configuration.StateUpdateIntervalMs,
            VisualizationUpdateIntervalMs = configuration.VisualizationUpdateIntervalMs,
            BatteryDischargeRate = configuration.BatteryDischargeRate,
            BatteryChargeRate = configuration.BatteryChargeRate,
            InitialBatteryCharge = configuration.InitialBatteryCharge,
            InitialBatteryChargeDeviation = configuration.InitialBatteryChargeDeviation,
            MaxSpeed = configuration.MaxSpeed
        };
    }

    private static async Task<Ok<AgvTypeConfigurationDto>> AddAsync(
        AddAgvTypeConfigurationDto configuration,
        AgvTypeConfigurationService service,
        ILoggerFactory loggerFactory)
    {
        loggerFactory.CreateLogger(LoggerCategory)
            .LogInformation("API to add the simulation configuration of agv type {AgvTypeIdentifier} called", configuration.AgvTypeIdentifier);
        return TypedResults.Ok(ToDto(await service.AddAsync(configuration)));
    }

    private static async Task<Ok<AgvTypeConfigurationDto>> UpdateAsync(
        Guid agvTypeId,
        UpdateAgvTypeConfigurationDto configuration,
        AgvTypeConfigurationService service,
        ILoggerFactory loggerFactory)
    {
        loggerFactory.CreateLogger(LoggerCategory)
            .LogInformation("API to update the simulation configuration of agv type {AgvTypeId} called", agvTypeId);
        return TypedResults.Ok(ToDto(await service.UpdateAsync(agvTypeId, configuration)));
    }

    private static async Task<NoContent> DeleteAsync(
        Guid agvTypeId,
        AgvTypeConfigurationService service,
        ILoggerFactory loggerFactory)
    {
        loggerFactory.CreateLogger(LoggerCategory)
            .LogInformation("API to delete the simulation configuration of agv type {AgvTypeId} called", agvTypeId);
        await service.DeleteAsync(agvTypeId);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<AgvTypeConfigurationDto>> GetAsync(Guid agvTypeId, AgvTypeConfigurationService service)
    {
        return TypedResults.Ok(ToDto(await service.GetRequiredAsync(agvTypeId)));
    }

    private static async Task<Ok<List<AgvTypeConfigurationDto>>> GetAllAsync(AgvTypeConfigurationService service)
    {
        return TypedResults.Ok((await service.GetAllAsync()).Select(ToDto).ToList());
    }
}
