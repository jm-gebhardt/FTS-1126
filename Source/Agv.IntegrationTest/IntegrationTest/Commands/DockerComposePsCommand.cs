using Gebhardt.Acc.IntegrationTest.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Gebhardt.Acc.IntegrationTest.Commands;

public class DockerComposePsCommand : DockerComposeCommand
{
    private List<DockerContainer>? _containers;

    public DockerComposePsCommand(string[] composeFiles, string envFilePath, ILogger logger, bool useAParameter = true)
        : base($"ps {(useAParameter ? "-a " : "")}--no-trunc --format json", composeFiles, envFilePath, logger)
    {
    }

    private static List<DockerContainer> ParseContainers(string stdout, ILogger logger)
    {
        var containers = new List<DockerContainer>();

        using var reader = new StringReader(stdout);
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Contains("level=warning msg="))
            {
                continue;
            }

            try
            {
                var container = JsonConvert.DeserializeObject<DockerContainer>(line);
                if (container != null)
                {
                    containers.Add(container);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse line: {Line}", line);
            }
        }

        return containers;
    }

    public async Task<List<DockerContainer>?> ParseContainersAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (!await ExecuteAsync(timeout, cancellationToken))
        {
            return null;
        }

        _containers = ParseContainers(Command.Stdout, Logger);

        return _containers;
    }
}