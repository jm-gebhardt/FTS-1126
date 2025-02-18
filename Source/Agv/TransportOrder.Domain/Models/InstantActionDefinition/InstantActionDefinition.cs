using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.InstantActionDefinition;

/// <summary>
/// Represents a definition of an instant action that can be run by agvs.
/// </summary>
public class InstantActionDefinition : AgvInstantAction
{
    /// <summary>
    /// The correlation id of the instant action definition.
    /// </summary>
    public Guid CorrelationId { get; init; } = Guid.Empty;

    /// <summary>
    /// The correlation ids of agvs that can run the instant action.
    /// </summary>
    public ICollection<Guid> AgvCorrelationIds { get; init; } = [];

    /// <inheritdoc cref="InstantActionDefinition"/>>
    public InstantActionDefinition()
    {
    }

    /// <inheritdoc cref="InstantActionDefinition"/>>
    public InstantActionDefinition(Guid correlationId, AgvInstantAction agvInstantActionDefinition)
    {
        CorrelationId = correlationId;
        Description = agvInstantActionDefinition.Description;
        ResultDescription = agvInstantActionDefinition.ResultDescription;
        ActionType = agvInstantActionDefinition.ActionType;
        BlockingTypes = agvInstantActionDefinition.BlockingTypes;
        Parameters = agvInstantActionDefinition.Parameters;
    }

    /// <summary>
    /// Add an agv to the instant action definition.
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    public void AddAgv(Guid agvCorrelationId)
    {
        AgvCorrelationIds.Add(agvCorrelationId);
    }

    /// <summary>
    /// Validate if instant action can be applied to given agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <exception cref="AgvInstantActionDefinitionNotFoundException">If agv can not run this instant action.</exception>
    public void ValidateAgv(Guid agvCorrelationId)
    {
        if (!AgvCorrelationIds.Contains(agvCorrelationId))
        {
            throw new AgvInstantActionDefinitionNotFoundException(ActionType, agvCorrelationId);
        }
    }

    /// <summary>
    /// Validate if given blocking type can be used with this instant action.
    /// </summary>
    /// <param name="blockingType">The blocking type.</param>
    /// <exception cref="InvalidInstantActionBlockingTypeException"></exception>
    public void ValidateBlockingType(ActionBlockingType? blockingType)
    {
        if (BlockingTypes == null && blockingType == null)
        {
            return;
        }

        bool noBlockingTypeGivenButIsNeeded = blockingType == null && BlockingTypes != null && !BlockingTypes.Contains(ActionBlockingType.None);
        if (noBlockingTypeGivenButIsNeeded)
        {
            throw new InvalidInstantActionBlockingTypeException(blockingType, ActionType);
        }

        bool blockingTypeGivenButNoneDefined = blockingType != null && BlockingTypes == null;
        if (blockingTypeGivenButNoneDefined)
        {
            throw new InvalidInstantActionBlockingTypeException(blockingType, ActionType);
        }

        bool blockingTypeNotSupported = blockingType != null && BlockingTypes != null && !BlockingTypes.Contains(blockingType.Value);
        if (blockingTypeNotSupported)
        {
            throw new InvalidInstantActionBlockingTypeException(blockingType, ActionType);
        }
    }

    /// <summary>
    /// Validate if parameter values can be used to start this action.
    /// </summary>
    /// <param name="parameterValues">Parameter values.</param>
    /// <exception cref="InstantActionParameterValueNotFoundException">Raised if a value was not set, but expected.</exception>
    /// <exception cref="InvalidInstantActionParameterException">Raised, if an invalid parameter was set.</exception>
    public void ValidateParameters(Dictionary<string, object>? parameterValues)
    {
        if (Parameters == null && parameterValues == null)
        {
            return;
        }

        if (Parameters == null && parameterValues != null)
        {
            throw new InstantActionParameterValueNotFoundException(ActionType);
        }
        
        foreach (var parameter in Parameters!.Where(p => !p.IsOptional))
        {
            if (!parameterValues!.Keys.Contains(parameter.Key))
            {
                throw new InstantActionParameterValueNotFoundException(parameter.Key, ActionType);
            }
        }

        foreach (string parameter in parameterValues!.Keys.Where(parameter => Parameters!.All(p => p.Key != parameter)))
        {
            throw new InvalidInstantActionParameterException(parameter, ActionType);
        }
    }

    /// <summary>
    /// Validate if given start values can be used with this instant action.
    /// </summary>
    /// <param name="agvCorrelationId">The gav correlation id.</param>
    /// <param name="blockingType">The blocking type.</param>
    /// <param name="parameters">The parameters.</param>
    public void ValidateStartValues(Guid agvCorrelationId, ActionBlockingType? blockingType, Dictionary<string, object>? parameters)
    {
        ValidateAgv(agvCorrelationId);
        ValidateBlockingType(blockingType);
        ValidateParameters(parameters);
    }
}