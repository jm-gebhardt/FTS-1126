using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.IntegrationTest.Commands;

public class DockerCommand : Command
{
    public DockerCommand(string args, ILogger logger, string? stdIn = null) : base($"docker {args}", logger, stdIn)
    {
    }
}