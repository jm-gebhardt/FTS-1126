using Newtonsoft.Json;

namespace Gebhardt.Acc.IntegrationTest.Models;

public class DockerContainer
{
    [JsonProperty("ID")] public string Id { get; set; } = "";

    [JsonProperty("Name")] public string Name { get; set; } = "";

    [JsonProperty("State")] public ContainerState State { get; set; }

    [JsonProperty("Status")] public string Status { get; set; } = "";

    [JsonProperty("Health")] public ContainerHealth? Health { get; set; } = ContainerHealth.Unknown;
}