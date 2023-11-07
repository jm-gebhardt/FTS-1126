using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;

/// <summary>
/// Configuration on how an IO should behave. E.g. what action should be executed when the IO is in a certain state for a certain time
/// </summary>
public class IoActionConfig
{
    /// <summary>
    /// Id of IO
    /// </summary>
    public string Id
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// For this time the IO must be stable until the action is executed
    /// </summary>
    public TimeSpan After
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// Required state of IO under which the action will execute
    /// </summary>
    public bool State
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// Action Type of IO Action
    /// </summary>
    public ActionTypes ActionType
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// Parameters for the action
    /// </summary>
    public List<object> Parameters
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// Deserialization
    /// </summary>
    [UsedImplicitly]
    public IoActionConfig()
    {
        Id = null!;
        Parameters = null!;
    }

    /// <inheritdoc cref="IoActionConfig"/>
    /// <param name="id">Id of IO</param>
    /// <param name="after">For this time the IO must be stable until the action is executed</param>
    /// <param name="state">Required state of IO under which the action will execute</param>
    /// <param name="actionType">Action Type of IO Action</param>
    /// <param name="parameters">Parameters for the action</param>
    public IoActionConfig(string id, TimeSpan after, bool state, ActionTypes actionType, List<object> parameters)
    {
        Id = id;
        After = after;
        State = state;
        ActionType = actionType;
        Parameters = parameters;
    }
}