using Gebhardt.Acc.IntegrationTest.Helpers;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.IntegrationTest.Commands;

public class DockerComposeCommand
{
    protected ILogger Logger { get; }
    protected Command Command { get; }

    public DockerComposeCommand(string args, string[] composeFiles, string envFilePath, ILogger logger)
    {
        Logger = logger;
        var projectName = DockerProjectNameGenerator.FromComposeFiles(composeFiles);
        var composeFilesFinal = $"-p {projectName} -f {string.Join(" -f ", composeFiles.Select(PathHelper.ToDockerPath))}";
        var envFile = PathHelper.ToDockerPath(envFilePath);

        Command = new Command($"docker compose {composeFilesFinal} --env-file \"{envFile}\" {args}", logger);
    }

    public async Task<bool> ExecuteAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return await Command.ExecuteAsync(timeout, cancellationToken);
    }
}