using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Converters;
using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;
using YamlDotNet.Serialization;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunicationTest.Helpers;

public static class ConfigGenerator
{
    public static async Task<Config> GenerateConfigAsync()
    {
        string yamlContent = await File.ReadAllTextAsync("TestData/module_config.yml");
        var config = new DeserializerBuilder()
            .WithTypeConverter(new ParameterYamlConverter())
            .Build()
            .Deserialize<Config>(yamlContent);

        return config;
    }
}