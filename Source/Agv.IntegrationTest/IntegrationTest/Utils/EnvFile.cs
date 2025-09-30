using System.Diagnostics;
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

    /// <summary>
    /// File path of generated environment file.
    /// </summary>
    public string? EnvFilePath { get; set; }

    public string RabbitMqUser { get; set; } = null!;
    public string RabbitMqPassword { get; set; } = null!;
    public string DatabaseUser { get; set; } = null!;
    public string DatabasePassword { get; set; } = null!;
    public string KeycloakClientId { get; set; } = null!;
    public string KeycloakUsername { get; set; } = null!;
    public string KeycloakUserPassword { get; set; } = null!;

    /// <summary>
    /// EnvFile Constructor
    /// </summary>
    /// <param name="binFilePath"></param>
    /// <param name="logger"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public EnvFile(string binFilePath, ILogger logger)
    {
        _binFilePath = binFilePath;
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

    private static string GetValue(List<string> lines, string key)
    {
        return lines.Single(l => l.StartsWith($"{key}=")).Split('=')[1].Trim('"');
    }
}