using Gebhardt.Acc.IntegrationTest.Commands;
using Gebhardt.Acc.IntegrationTest.Helpers;
using Gebhardt.Acc.IntegrationTest.Models;
using Microsoft.Extensions.Logging;
using Polly;

namespace Gebhardt.Acc.IntegrationTest.Utils;

/// <summary>
/// Control docker compose 
/// </summary>
public class DockerCompose
{
    private static readonly string[] ExcludedContainers =
        ["mssql.config", "sqlpad", "provisioning", "init", "tempo", "otel"];

    private readonly ILogger _logger;
    private readonly string[] _composeFiles;
    private readonly string _dockerRegistry;
    private readonly string _dockerRegistryRepository;
    private readonly string _dockerRegistryUser;
    private readonly string _dockerRegistryPassword;
    public EnvFile EnvFile { get; }

    /// <summary>
    /// Constructor
    /// <param name="composeFilesLocations">The list of compose files environment variable names</param>
    /// <param name="dockerRegistry">The name of the docker registry where to pull specific containers from</param>
    /// <param name="dockerRegistryRepository">The name of the docker repository where to pull specific containers from</param>
    /// <param name="dockerRegistryUser">The user for the docker registry</param>
    /// <param name="dockerRegistryPassword">The password for the docker registry</param>
    /// <param name="nugetPat">The PAT for Nuget</param>
    /// <param name="envFile">The env file to use. If null, the default env file will be created</param>
    /// </summary>
    public DockerCompose(string[] composeFilesLocations, string dockerRegistry = "",
        string dockerRegistryRepository = "", string dockerRegistryUser = "",
        string dockerRegistryPassword = "", string nugetPat = "", EnvFile? envFile = null)
    {
        var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
        _composeFiles = composeFilesLocations
            .SelectMany(e => EnvHelper.GetRequiredEnvVar(e).Split(',').Select(f => f.Trim().Trim('"'))).ToArray();
        _dockerRegistry = dockerRegistry;
        _dockerRegistryRepository = dockerRegistryRepository;
        _dockerRegistryUser = dockerRegistryUser;
        _dockerRegistryPassword = dockerRegistryPassword;

        _logger = loggerFactory.CreateLogger<DockerCompose>();
        var agvDevCombPath = FindAgvDevCombPath();

        EnvFile = envFile ?? new EnvFile(Path.Combine(agvDevCombPath, "bin", "17_create_kaeserkaris_env.sh"), nugetPat,
            _logger);
    }

    public Task InitializeAsync() => EnvFile.CreateAsync();

    private static string FindAgvDevCombPath()
    {
        var agvDevCombPath = Environment.CurrentDirectory;
        while (Directory.Exists(agvDevCombPath) && Path.GetFileName(agvDevCombPath) != "AgvDevComb")
        {
            Console.WriteLine($"Checking directory: {agvDevCombPath}");
            Console.WriteLine($"Files/Folders: {string.Join(", ", Directory.GetFileSystemEntries(agvDevCombPath))}");
            
            agvDevCombPath = Directory.GetParent(agvDevCombPath)?.FullName ?? string.Empty;
        }

        return agvDevCombPath;
    }

    /// <summary>
    /// <example>default_transportoder_1 -> transportorder</example>
    /// </summary>
    /// <param name="containerName"></param>
    /// <returns></returns>
    private static string GetServiceName(string containerName)
    {
        var parts = containerName.Split('_');
        return parts.Length switch
        {
            < 2 => containerName,
            >= 3 when int.TryParse(parts[^1], out _) => parts[^2],
            _ => parts[^1]
        };
    }


    /// <summary>
    /// Login to registry
    /// </summary>
    /// <returns></returns>
    public async Task<bool> LoginAsync()
    {
        var loginCommand =
            new DockerLoginCommand(_dockerRegistry, _dockerRegistryUser, _dockerRegistryPassword, _logger);
        if (!await loginCommand.ExecuteAsync(TimeSpan.FromSeconds(30)))
        {
            _logger.LogError("docker login failed: {Error}", loginCommand.Stderr);
            return false;
        }

        _logger.LogInformation("Docker login success: {Output}", loginCommand.Stdout);

        return true;
    }

    /// <summary>
    /// Pull containers
    /// </summary>
    /// <returns></returns>
    public async Task<bool> PullAsync()
    {
        var pullCommand = new DockerCommand($"pull {_dockerRegistryRepository}", _logger);
        var result = await pullCommand.ExecuteAsync(TimeSpan.FromMinutes(5));
        return result;
    }

    /// <summary>
    /// Create Containers
    /// </summary>
    public async Task<bool> UpAsync(string specificContainers = "")
    {
        var dockerCommand = new DockerComposeCommand(
            $"up{(!string.IsNullOrEmpty(specificContainers) ? $" {specificContainers}" : "")} -d",
            _composeFiles,
            EnvFile.EnvFilePath!,
            _logger);
        var result = await dockerCommand.ExecuteAsync(TimeSpan.FromMinutes(10));

        result &= await AreRunningAsync(ExcludedContainers);

        return result;
    }

    /// <summary>
    /// Removes everything
    /// </summary>
    /// <returns>true if command execution was successful</returns>
    public async Task<bool> DownAsync()
    {
        var dockerCommand =
            new DockerComposeCommand("down -v --remove-orphans", _composeFiles, EnvFile.EnvFilePath!, _logger);
        var mainDown = await dockerCommand.ExecuteAsync(TimeSpan.FromMinutes(10));

        var removeNetworkCommand = new DockerCommand("network rm agv_net", _logger);
        // the network might be removed already, so just ignore the result
        await removeNetworkCommand.ExecuteAsync(TimeSpan.FromSeconds(10));
        return mainDown;
    }

    /// <summary>
    /// Check if all containers are removed.
    /// </summary>
    /// <returns>true if all containers have been removed</returns>
    public async Task<bool> AreRemovedAsync(bool useAParameter = true)
    {
        var psCommand = new DockerComposePsCommand(_composeFiles, EnvFile.EnvFilePath!, _logger, useAParameter);

        var containers = await psCommand.ParseContainersAsync();
        if (containers == null)
        {
            return false;
        }

        if (containers.Count > 0)
        {
            _logger.LogError("Still running containers: {RunningContainers}",
                string.Join(", ", containers.Select(c => c.Name)));
            return false;
        }

        return true;
    }

    private async Task<bool> AreRunningAsync(string[]? excludedContainers = null, TimeSpan? timeout = null)
    {
        var defaultSleepTime = TimeSpan.FromSeconds(5);
        var defaultTimeout = TimeSpan.FromMinutes(1);
        var retryCount = (int)((timeout ?? defaultTimeout).TotalSeconds / defaultSleepTime.TotalSeconds);

        var policy = Policy
            .HandleResult<bool>(result => result == false)
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: _ => defaultSleepTime,
                onRetry: (_, timespan, retryAttempt, _) =>
                {
                    _logger.LogInformation(
                        "Waiting for all containers to be running... Retry {RetryAttempt} in {SleepDuration}",
                        retryAttempt, timespan);
                });

        var result = await policy.ExecuteAsync(async () =>
        {
            var psCommand = new DockerComposePsCommand(_composeFiles, EnvFile.EnvFilePath!, _logger);

            var containers = await psCommand.ParseContainersAsync();
            if (containers == null)
            {
                return false;
            }

            var failedContainers = containers
                .Where(c => !excludedContainers?.Any(ex => c.Name.Contains(ex)) ?? true)
                .Where(c => c.State != ContainerState.Running || c.Health != ContainerHealth.Healthy);

            var success = !failedContainers.Any() && containers.Count > 0;
            return success;
        });

        return result;
    }
}