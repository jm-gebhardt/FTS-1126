namespace Gebhardt.Acc.IntegrationTest.Utils;

/// <summary>
/// Env Variable names
/// </summary>
public static class EnvVars
{
    /// <summary>
    /// Compose file path
    /// </summary>
    public const string ComposeFileEnvVar = "COMPOSE_FILE_IntegrationTest";
        
    /// <summary>
    /// Compose file path
    /// </summary>
    public const string ComposeFileEnvVarVirtualAgv = "COMPOSE_FILE_VirtualAGV";
        
    /// <summary>
    /// MQTT Samples
    /// </summary>
    public const string MqttSamplePath = "MQTT_SAMPLES";

    /// <summary>
    /// TAR's Docker Registry Password
    /// </summary>
    public const string DockerRegistryPassword = "DockerRegistryPassword";
}