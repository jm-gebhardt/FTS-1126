using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gebhardt.StoreWare.Agv.IntegrationTest.Utils
{
    /// <summary>
    /// Control docker compose 
    /// </summary>
    public class DockerCompose
    {
        private readonly EnvFile _envFile;
        
        private readonly ILogger _logger;

        private readonly string _composeFile =
            Environment.GetEnvironmentVariable(EnvVars.ComposeFileEnvVar) 
            ?? throw new InvalidOperationException($"Environment variable {EnvVars.ComposeFileEnvVar} must be set.");
        
        /// <summary>
        /// Constructor
        /// </summary>
        public DockerCompose()
        {
            var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
            _logger = loggerFactory.CreateLogger<DockerCompose>();
            _envFile = new EnvFile(_composeFile);
            _envFile.Create();
        }

        /// <summary>
        /// Create Containers
        /// </summary>
        public bool Up()
        {
            return RunCommand("up -d", 240000);
        }

        /// <summary>
        /// Stop Containers
        /// </summary>
        /// <returns>true if command execution was successful</returns>
        public bool Stop()
        {
            return RunCommand("stop", 60000);
        }

        /// <summary>
        /// Removes everything
        /// </summary>
        /// <returns>true if command execution was successful</returns>
        public bool Down()
        {
            return RunCommand("down -v");
        }

        /// <summary>
        /// Are all containers removed
        /// </summary>
        /// <returns>true if all containers have been removed</returns>
        public bool AreRemoved()
        {
            var success = RunCommand("ps -a --format=json", out var stdout);
            if (!success)
            {
                return false;
            }
            return stdout == "[]";
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
            
            foreach (JObject item in JArray.Parse(stdout))
            {
                if(item["State"].ToString() != "running")
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Run a docker compose command, output will be printed to stdout/stderr
        /// </summary>
        /// <param name="args"></param>
        /// <param name="timeout"></param>
        public bool RunCommand(string args, int timeout=60000)
        {
            return RunCommand(args, out _, timeout);
        }

        /// <summary>
        /// Run a docker command and write output to stdout variable
        /// </summary>
        /// <param name="args"></param>
        /// <param name="timeout"></param>
        /// <param name="stdout"></param>
        /// <returns>Returns true if exit code is zero</returns>
        public bool RunCommand(string args, out string stdout, int timeout=60000)
        {
            var cmdArgs = $"compose -f \"{_composeFile}\" --env-file \"{_envFile.FilePath}\" " + args;
            _logger.LogInformation("Executing docker {CommandArguments}", args);
            
            var processInfo = new ProcessStartInfo("docker", cmdArgs)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process();
            using var stdOutWriter = new StringWriter();
            process.StartInfo = processInfo;
            process.OutputDataReceived += (sender, e) =>
            {
               Console.WriteLine(e.Data);
               stdOutWriter.Write(e.Data);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                Console.Error.WriteLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit(timeout);
            stdout = stdOutWriter.ToString();
            if (!process.HasExited)
            {
                process.Kill();
                _logger.LogError("Command timeout");
                process.WaitForExit();
                return false;
            }

            var exitCode = process.ExitCode;
            process.Close();
            return exitCode == 0;
        }
    }
}