using Gebhardt.Acc.IntegrationTest.Commands;
using Gebhardt.Acc.IntegrationTest.Helpers;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.IntegrationTest.Utils;

/// <summary>
/// Generates Environment file via bin script
/// </summary>
public class EnvFile
{
    private readonly ILogger _logger;

    private readonly string _binFilePath;
    private readonly string _nugetPat;

    /// <summary>
    /// File path of generated environment file.
    /// </summary>
    public string? EnvFilePath { get; private set; }

    public string RabbitMqUser { get; private set; } = null!;
    public string RabbitMqPassword { get; private set; } = null!;
    public string DatabaseUser { get; private set; } = null!;
    public string DatabasePassword { get; private set; } = null!;
    public string KeycloakClientId { get; private set; } = null!;
    public string KeycloakUsername { get; private set; } = null!;
    public string KeycloakUserPassword { get; private set; } = null!;

    /// <summary>
    /// EnvFile Constructor
    /// </summary>
    /// <param name="binFilePath"></param>
    /// <param name="nugetPat"></param>
    /// <param name="logger"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public EnvFile(string binFilePath, string nugetPat, ILogger logger)
    {
        _binFilePath = binFilePath;
        _nugetPat = nugetPat;
        _logger = logger;
    }

    /// <summary>
    /// Generates Env file via script.
    /// <returns>File path of generated env file</returns>
    /// </summary>
    public async Task CreateAsync()
    {
        if (EnvFilePath != null)
        {
            return;
        }

        var result = new Command($"yes | {PathHelper.ToDockerPath(_binFilePath)}", _logger);

        if (!await result.ExecuteAsync(TimeSpan.FromSeconds(10)))
        {
            throw new Exception("Failed to create env file");
        }

        EnvFilePath = Path.GetTempFileName();

        var parent = Directory.GetParent(_binFilePath)!.FullName;
        parent = Directory.GetParent(parent)!.FullName;
        var envFilePath = Path.Combine(parent, ".env");

        InjectVariables(envFilePath);

        File.Copy(envFilePath, EnvFilePath, true);

        var lines = (await File.ReadAllLinesAsync(EnvFilePath)).ToList();
        RabbitMqUser = GetValue(lines, "RABBITMQ_USER");
        RabbitMqPassword = GetValue(lines, "RABBITMQ_PASSWORD");
        DatabaseUser = GetValue(lines, "MSSQL_DEFAULT_USER");
        DatabasePassword = GetValue(lines, "MSSQL_DEFAULT_USER_PASSWORD");
        KeycloakClientId = GetValue(lines, "KEYCLOAK_CLIENTID");
        KeycloakUsername = "agv";
        KeycloakUserPassword = "agv";
    }

    private void InjectVariables(string envFilePath)
    {
        var lines = File.ReadAllLines(envFilePath).ToList();

        SetVariable(lines, EnvVars.NugetPat, _nugetPat);

        File.WriteAllLines(envFilePath, lines);
    }

    private void SetVariable(List<string> lines, string key, string value)
    {
        var entry = $"{key}={value}";
        var index = lines.FindIndex(l => l.StartsWith($"{key}="));

        if (index != -1)
        {
            lines[index] = entry;
        }
        else
        {
            lines.Add(entry);
        }
    }

    private static string GetValue(List<string> lines, string key)
    {
        return lines.Single(l => l.StartsWith($"{key}=")).Split('=')[1].Trim('"');
    }
}