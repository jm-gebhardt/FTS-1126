using Gebhardt.Acc.SmartLinkCommunication.Converters;
using Gebhardt.Acc.SmartLinkCommunication.Models;
using YamlDotNet.Serialization;

namespace Gebhardt.Acc.SmartLinkCommunicationTest.Helpers;

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