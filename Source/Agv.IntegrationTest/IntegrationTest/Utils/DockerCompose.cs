using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;

namespace Gebhardt.Acc.IntegrationTest.Utils;

/// <summary>
/// Control docker compose 
/// </summary>
public class DockerCompose
{
    private readonly EnvFile _envFile;

    private readonly ILogger _logger;

    private readonly string[] _composeFiles;

    private readonly string _dockerRegistry;

    private readonly string _dockerRegistryRepository;

    private readonly string _dockerRegistryUser;

    private readonly string _dockerRegistryPassword;

    /// <summary>
    /// Constructor
    /// <param name="composeFilesLocation">The list of compose files locations</param>
    /// <param name="dockerRegistry">The name of the docker registry where to pull specific containers from</param>
    /// <param name="dockerRegistryRepository">The name of the docker repository where to pull specific containers from</param>
    /// <param name="dockerRegistryUser">The user for the docker registry</param>
    /// <param name="dockerRegistryPassword">The password for the docker registry</param>
    /// </summary>
    public DockerCompose(string composeFilesLocation, string dockerRegistry = "",
        string dockerRegistryRepository = "", string dockerRegistryUser = "", string dockerRegistryPassword = "")
    {
        var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
        _composeFiles = Environment.GetEnvironmentVariable(composeFilesLocation)?.Split(',')
                        ?? throw new InvalidOperationException(
                            $"Environment variable {composeFilesLocation} must be set.");
        _dockerRegistry = dockerRegistry;
        _dockerRegistryRepository = dockerRegistryRepository;
        _dockerRegistryUser = dockerRegistryUser;
        _dockerRegistryPassword = dockerRegistryPassword;

        _logger = loggerFactory.CreateLogger<DockerCompose>();
        _envFile = new EnvFile(_composeFiles.First());
        _envFile.Create();
    }

    /// <summary>
    /// Login to registry
    /// </summary>
    /// <returns></returns>
    public bool Login()
    {
        return RunDockerCommand($"login {_dockerRegistry} -u {_dockerRegistryUser} -p {_dockerRegistryPassword}");
    }

    /// <summary>
    /// Pull containers
    /// Timeout 300s
    /// </summary>
    /// <returns></returns>
    public bool Pull()
    {
        return RunDockerCommand($"pull {_dockerRegistryRepository}", 300000);
    }

    /// <summary>
    /// Create Containers
    /// </summary>
    public bool Up(string specificContainers = "")
    {
        return RunDockerComposeCommand($"up {specificContainers} -d", 640000);
    }

    /// <summary>
    /// Stop Containers
    /// </summary>
    /// <returns>true if command execution was successful</returns>
    public bool Stop()
    {
        return RunDockerComposeCommand("stop");
    }

    /// <summary>
    /// Removes everything
    /// </summary>
    /// <returns>true if command execution was successful</returns>
    public bool Down()
    {
        return RunDockerComposeCommand("down -v --remove-orphans");
    }

    /// <summary>
    /// Are all containers removed
    /// </summary>
    /// <returns>true if all containers have been removed</returns>
    public bool AreRemoved()
    {
        var success = RunCommand("ps -a --format=json", out var stdout);
        Trace.WriteLine($"Running docker container: {stdout}");

        if (!success)
        {
            return false;
        }

        // Due to https://github.com/docker/compose/issues/10958 check for both
        return stdout is "[]" or "";
    }

    /// <summary>
    /// Are all started services in state "running" 
    /// </summary>
    /// <returns>true if all containers are running</returns>
    public bool AreRunning(string[]? excludedContainers = null)
    {
        var stdout = "";
        excludedContainers ??= Array.Empty<string>();
        var polly = Policy
            .HandleResult<bool>(c => c == false || stdout == string.Empty)
            .WaitAndRetry(5, (_, retryCount) =>
            {   
                _logger.LogError("try: {RetryCount}, Stdout: {Stdout}", retryCount, stdout);
                return TimeSpan.FromSeconds(2);
            });

        // Due to https://github.com/docker/compose/issues/10958 jq is used to fix the json
        var success = polly.Execute(() =>
            RunCommand(" ps --no-trunc --format '{\"name\":\"{{.Names}}\",\"state\":\"{{.State}}\"}' | jq -sc",
                out stdout));


        if (!success)
        {
            return false;
        }

        var containers = JsonConvert.DeserializeObject<List<DockerContainer>>(stdout);

        var failedContainers = containers?.Where(c => !excludedContainers.Any(e => c.Name.EndsWith(e)))
            .Where(c => !c.State.Equals("running"));

        return failedContainers?.Count() == 0;
    }

    /// <summary>
    /// Run a docker compose command, output will be printed to stdout/stderr
    /// </summary>
    /// <param name="args"></param>
    /// <param name="timeout"></param>
    private bool RunDockerComposeCommand(string args, int timeout = 120000)
    {
        return RunCommand(args, out _, true, timeout);
    }

    /// <summary>
    /// Run a docker compose command, output will be printed to stdout/stderr
    /// </summary>
    /// <param name="args"></param>
    /// <param name="timeout"></param>
    private bool RunDockerCommand(string args, int timeout = 120000)
    {
        return RunCommand(args, out _, false, timeout);
    }

    /// <summary>
    /// Run a docker command and write output to stdout variable
    /// </summary>
    /// <param name="args"></param>
    /// <param name="dockerComposeCommand"></param>
    /// <param name="timeout"></param>
    /// <param name="stdout"></param>
    /// <returns>Returns true if exit code is zero</returns>
    private bool RunCommand(string args, out string stdout, bool dockerComposeCommand = true, int timeout = 120000)
    {
        var cmdOutput = new StringBuilder();
        var composeFiles = $"-f {string.Join(" -f ", _composeFiles)}";
        var cmd = dockerComposeCommand
            ? $"docker compose {composeFiles} --env-file \"{_envFile.FilePath}\" "
            : " docker ";

        _logger.LogInformation("Executing {Command}{CommandArguments}", cmd, args);

        using var process = new Process()
        {
            StartInfo =
            {
                FileName = "bash",
                ArgumentList = { "-c", "--", cmd + args },
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        
        using var outputWaitHandle = new AutoResetEvent(false);
        process.OutputDataReceived += (sender, e) =>
        {
            Console.WriteLine(e.Data);
            
            if (e.Data == null)
            {
                outputWaitHandle.Set();
            }
            else
            {
                cmdOutput.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            Console.Error.WriteLine(e.Data);
        };
           
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
            
            
        if (process.WaitForExit(timeout) &&
            outputWaitHandle.WaitOne(timeout))
        {
            stdout = cmdOutput.ToString();
            var exitCode = process.ExitCode;
            process.Close();
            return exitCode == 0;
        }

        process.Kill();
        _logger.LogError("Command timeout {CommandArguments}", args);
        process.WaitForExit();
        stdout = string.Empty;
        return false;
    }
}