using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.IntegrationTest.Commands;

public class DockerLoginCommand : DockerCommand
{
    public DockerLoginCommand(
        string dockerRegistry,
        string dockerRegistryUser,
        string dockerRegistryPassword,
        ILogger logger)
        : base($"login {dockerRegistry} -u {dockerRegistryUser} --password-stdin", logger, dockerRegistryPassword)
    {
    }
}