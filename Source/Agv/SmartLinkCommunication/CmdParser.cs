using CommandLine;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Converters;
using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication;

/// <summary>
/// Parses the command line arguments
/// </summary>
public static class CmdParser
{
    private const string PickDropNodeIdSeparator = ",";

    /// <inheritdoc cref="CmdParser"/>
    /// <param name="args"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static async Task<Config?> ParseAsync(string[] args, ILogger logger)
    {
        Config? config = null;

        await Parser.Default.ParseArguments<CmdConfig>(args)
            .WithParsedAsync(async c =>
            {
                if (string.IsNullOrEmpty(c.ConfigFilePath))
                {
                    // make sure no other config is set
                    if (c.ModuleUrl == null || c.Pin == null || c.PickDropNode == null)
                    {
                        logger.LogError("Config file not set and at least one other value is missing");
                        return;
                    }

                    string? pickNodeId = null;
                    string? dropNodeId = null;
                    if (c.PickDropNode != null)
                    {
                        string[] split = c.PickDropNode.Split(PickDropNodeIdSeparator);
                        if (split.Length != 2)
                        {
                            logger.LogError("Invalid pick drop argument.");
                            return;
                        }
                        pickNodeId = split[0];
                        dropNodeId = split[1];
                    }

                    if (string.IsNullOrEmpty(pickNodeId))
                    {
                        logger.LogError("Pick node id not set");
                        return;
                    }

                    if (string.IsNullOrEmpty(dropNodeId))
                    {
                        logger.LogError("Drop node id not set");
                        return;
                    }

                    if (c.WaitingTime <= 0)
                    {
                        logger.LogError("Waiting time must be greater than 0");
                        return;
                    }

                    if (string.IsNullOrEmpty(c.ModuleUrl))
                    {
                        logger.LogError("Module url not set");
                        return;
                    }

                    // create config
                    config = new Config(new List<ModuleConfig>()
                    {
                        new(c.ModuleUrl, new List<IoActionConfig>()
                        {
                            new(c.Pin, TimeSpan.FromSeconds(c.WaitingTime), false, ActionTypes.PublishQueueActionGroup, new List<object>()
                            {
                                new ActionDefinition(ActionType.Pick, pickNodeId),
                                new ActionDefinition(ActionType.Drop, dropNodeId)
                            })
                        })
                    });
                }
                else
                {
                    // make sure no other config is set
                    if (c.ModuleUrl != null || c.Pin != null || c.PickDropNode != null)
                    {
                        logger.LogError("Config file set together with other values. That's not supported.");
                    }

                    string yamlContent = await File.ReadAllTextAsync(c.ConfigFilePath);

                    config = new DeserializerBuilder()
                        .WithTypeConverter(new ParameterYamlConverter())
                        .Build()
                        .Deserialize<Config>(yamlContent);
                }
            });

        logger.LogInformation("Config: {config}", config);

        return config;
    }
}