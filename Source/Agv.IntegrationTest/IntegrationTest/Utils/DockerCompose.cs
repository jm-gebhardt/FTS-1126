using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.IntegrationTest.Utils;

/// <summary>
/// Control docker compose 
/// </summary>
public class DockerCompose
{
    private readonly EnvFile _envFile;

    private readonly ILogger _logger;

    private readonly string _composeFile;

    private readonly string _dockerRegistry;

    private readonly string _dockerRegistryRepository;

    private readonly string _dockerRegistryUser;

    private readonly string _dockerRegistryPassword;

    /// <summary>
    /// Constructor
    /// <param name="composeFileLocation">The location of the compose file</param>
    /// <param name="dockerRegistry">The name of the docker registry where to pull specific containers from</param>
    /// <param name="dockerRegistryRepository">The name of the docker repository where to pull specific containers from</param>
    /// <param name="dockerRegistryUser">The user for the docker registry</param>
    /// <param name="dockerRegistryPassword">The password for the docker registry</param>
    /// </summary>
    public DockerCompose(string composeFileLocation, string dockerRegistry = "",
        string dockerRegistryRepository = "", string dockerRegistryUser = "", string dockerRegistryPassword = "")
    {
        var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
        _composeFile = Environment.GetEnvironmentVariable(composeFileLocation)
                       ?? throw new InvalidOperationException(
                           $"Environment variable {composeFileLocation} must be set.");
        _dockerRegistry = dockerRegistry;
        _dockerRegistryRepository = dockerRegistryRepository;
        _dockerRegistryUser = dockerRegistryUser;
        _dockerRegistryPassword = dockerRegistryPassword;

        _logger = loggerFactory.CreateLogger<DockerCompose>();
        _envFile = new EnvFile(_composeFile);
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
        return RunDockerComposeCommand($"up {specificContainers} -d", 240000);
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
        return RunDockerComposeCommand("down -v");
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
    public bool AreRunning()
    {
        var success = RunCommand("ps -a --format=json", out var stdout);
        if (!success)
        {
            return false;
        }

        // Due to https://github.com/docker/compose/issues/10958 split string instead of using NewtonSoft JArray.Parse
        var states = stdout.Split("\"State\":");
        var firstString = true;
        foreach (var stateString in states)
        {
            if (firstString)
            {
                firstString = false;
                continue;
            }

            if (stateString.StartsWith("\"running\"")) continue;
            Trace.WriteLine("At least one docker container is not in state \"running\".");
            return false;
        }
        return true;
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
        var cmd = dockerComposeCommand ? $"compose -f \"{_composeFile}\" --env-file \"{_envFile.FilePath}\" " : " ";
            
        _logger.LogInformation("Executing docker {Command }{CommandArguments}", cmd, args);

        var processInfo = new ProcessStartInfo("docker", cmd + args)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process();
        var stdOutWriter = new StringWriter();
        process.StartInfo = processInfo;
        process.OutputDataReceived += (_, e) =>
        {
            Console.WriteLine(e.Data);
            stdOutWriter.Write(e.Data);
        };
        process.ErrorDataReceived += (_, e) => { Console.Error.WriteLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit(timeout);
        stdout = stdOutWriter.ToString();
        if (!process.HasExited)
        {
            process.Kill();
            _logger.LogError("Command timeout {CommandArguments}", args);
            process.WaitForExit();
            return false;
        }

        var exitCode = process.ExitCode;
        process.Close();
        return exitCode == 0;
    }
}