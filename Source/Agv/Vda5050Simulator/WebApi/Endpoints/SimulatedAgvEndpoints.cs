using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Vda5050Simulator.Configuration;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Gebhardt.Acc.Vda5050Simulator.Runtime;
using Gebhardt.Acc.Vda5050Simulator.WebApi.Controllers;
using Gebhardt.Acc.Vda5050Simulator.WebApi.Dtos;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Vda5050Simulator.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for managing simulated agvs.
/// </summary>
public static class SimulatedAgvEndpoints
{
    /// <summary>
    /// Maps the simulated agv endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapSimulatedAgvEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("agv/{correlationId:guid}")
            .RequireAuthorization()
            .WithGroupName(RestApiGroup.GroupName)
            .WithTags("SimulatedAgv");

        group.MapPatch("switchOperatingMode", SwitchOperatingMode)
            .WithName("SwitchOperatingMode")
            .WithSummary("Switch the operating mode of an agv.");
        group.MapPatch("move", SetToPosition)
            .WithName("SetToPosition")
            .WithSummary("Set the agv to the given position.");
        group.MapPost("load", AddLoad)
            .WithName("AddLoad")
            .WithSummary("Add a load to an agv.");
        group.MapDelete("load/{loadId}", RemoveLoad)
            .WithName("RemoveLoad")
            .WithSummary("Remove a load from an agv.");
        group.MapGet("state", GetState)
            .WithName("GetState")
            .WithSummary("Get the latest state of an agv.");

        return routes;
    }

    private static IResult SwitchOperatingMode(Guid correlationId, AgvOperatingMode operatingMode, FleetHost fleet)
    {
        if (!fleet.TryGetAgv(correlationId, out var id) || id == null)
        {
            return TypedResults.NotFound();
        }

        fleet.Dispatch(new SwitchOperatingModeCommand(id, operatingMode));
        return TypedResults.Ok();
    }

    private static async Task<IResult> SetToPosition(Guid correlationId, AgvPositionDto position, FleetHost fleet, CancellationToken cancellationToken)
    {
        if (!fleet.TryGetAgv(correlationId, out var id) || id == null)
        {
            return TypedResults.NotFound();
        }

        var result = await fleet.SendAsync(new SetPositionCommand(id, position.X, position.Y, position.Theta, position.MapId), cancellationToken);
        return ToResult(result);
    }

    private static async Task<IResult> AddLoad(Guid correlationId, AddLoadDto load, FleetHost fleet, CancellationToken cancellationToken)
    {
        if (!fleet.TryGetAgv(correlationId, out var id)|| id == null)
        {
            return TypedResults.NotFound();
        }

        var result = await fleet.SendAsync(new AddLoadCommand(id, load.LoadId, load.LoadType, load.LoadPosition), cancellationToken);
        return ToResult(result);
    }

    private static async Task<IResult> RemoveLoad(Guid correlationId, string loadId, FleetHost fleet, CancellationToken cancellationToken)
    {
        if (!fleet.TryGetAgv(correlationId, out var id) || id == null)
        {
            return TypedResults.NotFound();
        }

        var result = await fleet.SendAsync(new RemoveLoadCommand(id, loadId), cancellationToken);
        return ToResult(result);
    }

    private static IResult GetState(Guid correlationId, IAgvStateCache stateCache, IOptions<MqttOptions> mqttOptions)
    {
        if (!stateCache.TryGet(correlationId, out var id, out var state) || id == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(Vda5050StateDtoMapper.Map(id, state, mqttOptions.Value.ProtocolVersion));
    }

    private static IResult ToResult(CommandResult? result)
    {
        return result switch
        {
            null => TypedResults.NotFound(),
            { Success: true } => TypedResults.Ok(),
            _ => TypedResults.BadRequest(result.Error)
        };
    }
}
