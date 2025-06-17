using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;
using Gebhardt.Acc.Common.Domain.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gebhardt.Acc.DeviceCommunication.Common.Configuration;

/// <summary>
/// Configurations of instant actions.
/// </summary>
[PublicAPI]
public class InstantActionsConfiguration
{
    /// <summary>
    /// Instant action configurations.
    /// </summary>
    public List<AgvInstantActionConfiguration> InstantActions { get; set; } = [];

    /// <inheritdoc cref="InstantActionsConfiguration"/>>
    public InstantActionsConfiguration(List<AgvInstantActionConfiguration> instantActions)
    {
        InstantActions = instantActions;
    }

    /// <inheritdoc cref="InstantActionsConfiguration"/>>
    public InstantActionsConfiguration()
    {
    }

    /// <summary>
    /// Load instant action configuration using the given configuration reader.
    /// </summary>
    public void Load(IConfigurationReader reader)
    {
        using var content = reader.Read();
        if (content == null)
        {
            return;
        }
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        var yamlData = deserializer.Deserialize<InstantActionsConfiguration>(content);
        InstantActions = yamlData.InstantActions;
    }

    /// <summary>
    /// Get the instant action definitions for the given agv.
    /// </summary>
    /// <param name="agvData">The agv data.</param>
    /// <param name="agvSerialNumber">The agv serial number.</param>
    /// <returns></returns>
    public ICollection<AgvInstantAction> GetDefinitionsForAgv(AgvData agvData, string agvSerialNumber)
    {
        return InstantActions.Where(ia =>
                (ia.AgvSerialNumbers == null && ia.AgvTypes == null) ||
                (ia.AgvSerialNumbers?.Contains(agvSerialNumber)??false) ||
                (ia.AgvTypes != null && ia.AgvTypes.Any(at => at.Type == agvData.Type && at.Model == agvData.Model && !(at.ExcludedAgvSerialNumbers?.Contains(agvSerialNumber)??false))))
            .Select(ia => new AgvInstantAction()
            {
                Description = ia.Description,
                BlockingTypes = null,
                Parameters = ia.Parameters,
                ActionType = ia.ActionType,
                ResultDescription = ia.ResultDescription
            }).ToList();
    }
}

/// <summary>
/// Configuration of an instant action.
/// </summary>
[PublicAPI]
public class AgvInstantActionConfiguration : AgvInstantAction
{
    /// <summary>
    /// The agv types capable of this instant action.
    /// </summary>
    public List<AgvType>? AgvTypes { get; set; }

    /// <summary>
    /// Exclusive agvs that are capable of this instant action, in contrast to others of the same type
    /// (if given, do not include its type in <see cref="AgvTypes"/>).
    /// </summary>
    public List<string>? AgvSerialNumbers { get; set; } 
}

/// <summary>
/// Type of agv.
/// </summary>
[PublicAPI]
public record AgvType
{
    /// <summary>
    /// The type of the agv.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The model of the agv.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Serial numbers of agvs that are of this type and model, but 
    /// do not offer the instant action.
    /// </summary>
    public List<string>? ExcludedAgvSerialNumbers { get; set; } = [];
}
