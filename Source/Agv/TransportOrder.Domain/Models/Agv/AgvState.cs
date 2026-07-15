using Gebhardt.Acc.TransportOrder.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

/// <summary>
/// Agv State
/// </summary>
public enum AgvState
{
    /// <summary>
    /// Initial
    /// </summary>
    Initial,

    /// <summary>
    /// Discovered
    /// </summary>
    Discovered,

    /// <summary>
    /// Invalid configuration
    /// </summary>
    InvalidConfiguration,

    /// <summary>
    /// Idle
    /// </summary>
    Idle,

    /// <summary>
    /// Busy
    /// </summary>
    Busy
}

/// <summary>
/// provides extension methods for the agvs state.
/// </summary>
public static class AgvStateExtensions
{
    /// <summary>
    /// Transitions to new state from defined old state.
    /// </summary>
    /// <param name="oldState">The old state.</param>
    /// <param name="newState">The new state</param>
    /// <returns></returns>
    /// <exception cref="InvalidStateTransitionException"></exception>
    public static AgvState TransitionTo(this AgvState oldState, AgvState newState)
    {
        switch (newState)
        {
            case AgvState.Discovered when oldState is AgvState.Initial or AgvState.InvalidConfiguration:
            case AgvState.Idle when oldState is AgvState.Discovered or AgvState.Busy or AgvState.Idle or AgvState.InvalidConfiguration:
            case AgvState.InvalidConfiguration when oldState is AgvState.Discovered or AgvState.Idle or AgvState.Busy or AgvState.Initial or AgvState.InvalidConfiguration:
            case AgvState.Busy when oldState is AgvState.Idle:
                break;

            default:
                throw new InvalidStateTransitionException($"Cannot transition from state '{Enum.GetName(oldState)}' to '{Enum.GetName(newState)}'.");
        }

        return newState;
    }
}