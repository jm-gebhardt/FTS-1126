using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs.Filters;

/// <summary>
/// Checks if a <see cref="Vda5050StateDto"/> has changed significantly, ignoring volatile fields (header id, timestamps).
/// </summary>
public class Vda5050StateSignificanceChecker : ReflectionSignificanceChecker<Vda5050StateDto>
{
    private readonly Vda5050StateConfiguration _configuration;

    /// <inheritdoc cref="Vda5050StateSignificanceChecker"/>
    /// <param name="configuration">The configuration for the significance.</param>
    public Vda5050StateSignificanceChecker(Vda5050StateConfiguration configuration)
    {
        _configuration = configuration;
    }

    private static IEnumerable<string> NodeSignature(IEnumerable<NodeStateDto> nodes)
    {
        return nodes.Select(node => $"{node.NodeId}:{node.SequenceId}:{node.Released}");
    }

    private static IEnumerable<string> EdgeSignature(IEnumerable<EdgeStateDto> edges)
    {
        return edges.Select(edge => $"{edge.EdgeId}:{edge.SequenceId}:{edge.Released}");
    }

    private static IEnumerable<string> ActionSignature(IEnumerable<ActionStateDto> actions)
    {
        return actions.Select(action => $"{action.ActionId}:{action.ActionStatus}");
    }

    private static IEnumerable<string> ErrorSignature(IEnumerable<ErrorDto> errors)
    {
        return errors.Select(error => $"{error.ErrorType}:{error.ErrorLevel}");
    }

    private static IEnumerable<string> LoadSignature(IEnumerable<LoadDto>? loads)
    {
        return loads?.Select(load => load.LoadId ?? string.Empty) ?? [];
    }

    private bool HasSignificantPositionChange(Vda5050StateDto current, Vda5050StateDto previous)
    {
        var currentPosition = current.AgvPosition!;
        var previousPosition = previous.AgvPosition!;
        return Math.Abs(currentPosition.X - previousPosition.X) >= _configuration.PositionThreshold
               || Math.Abs(currentPosition.Y - previousPosition.Y) >= _configuration.PositionThreshold
               || Math.Abs(currentPosition.Theta - previousPosition.Theta) >= _configuration.ThetaThreshold;
    }

    /// <inheritdoc />
    protected override Dictionary<string, Func<Vda5050StateDto, Vda5050StateDto, bool>> GetSignificantChangeByPropertyName()
    {
        return new Dictionary<string, Func<Vda5050StateDto, Vda5050StateDto, bool>>
        {
            { nameof(Vda5050StateDto.HeaderId), (_, _) => false },
            { nameof(Vda5050StateDto.Timestamp), (_, _) => false },
            { nameof(Vda5050StateDto.MessageCreatedDate), (_, _) => false },
            { nameof(Vda5050StateDto.BatteryState), (current, previous) => Math.Abs(current.BatteryState.BatteryCharge - previous.BatteryState.BatteryCharge) >= _configuration.BatteryChargeThreshold },
            { nameof(Vda5050StateDto.AgvPosition), HasSignificantPositionChange },
            { nameof(Vda5050StateDto.NodeStates), (current, previous) => !NodeSignature(current.NodeStates).SequenceEqual(NodeSignature(previous.NodeStates)) },
            { nameof(Vda5050StateDto.EdgeStates), (current, previous) => !EdgeSignature(current.EdgeStates).SequenceEqual(EdgeSignature(previous.EdgeStates)) },
            { nameof(Vda5050StateDto.ActionStates), (current, previous) => !ActionSignature(current.ActionStates).SequenceEqual(ActionSignature(previous.ActionStates)) },
            { nameof(Vda5050StateDto.Errors), (current, previous) => !ErrorSignature(current.Errors).SequenceEqual(ErrorSignature(previous.Errors)) },
            { nameof(Vda5050StateDto.Loads), (current, previous) => !LoadSignature(current.Loads).SequenceEqual(LoadSignature(previous.Loads)) }
        };
    }
}