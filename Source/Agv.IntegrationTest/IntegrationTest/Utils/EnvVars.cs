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
    /// TAR's Docker Registry Url
    /// </summary>
    public const string DockerRegistryUrl = "DOCKER_REGISTRY_URL";

    /// <summary>
    /// TAR's Docker Registry Repository Url
    /// </summary>
    public const string DockerRegistryRepository = "DOCKER_REGISTRY_REPOSITORY";

    /// <summary>
    /// TAR's Docker Registry User Name
    /// </summary>
    public const string DockerRegistryUser = "DOCKER_REGISTRY_USER";

    /// <summary>
    /// TAR's Docker Registry Password
    /// </summary>
    public const string DockerRegistryPassword = "DOCKER_REGISTRY_PASSWORD";

    /// <summary>
    /// Default keycloak client ID.
    /// This client is required to retrieve valid JWT tokens
    /// </summary>
    public const string KeycloakClientId = "KEYCLOAK_DEFAULT_CLIENT_ID";

    /// <summary>
    /// Default keycloak username.
    /// This username is required to retrieve valid JWT tokens
    /// </summary>
    public const string KeycloakUsername = "KEYCLOAK_DEFAULT_USERNAME";

    /// <summary>
    /// Default keycloak user password
    /// This username and password are required to retrieve valid JWT tokens
    /// </summary>
    public const string KeycloakUserPassword = "KEYCLOAK_DEFAULT_USERPASSWORD";
}