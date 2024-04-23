using System.Text.Json;
using Gebhardt.Acc.SmartLinkCommunication.Models;

namespace Gebhardt.Acc.SmartLinkCommunicationTest.Helpers;

public static class SmartLinkDataGenerator
{
    public static async Task<string> GenerateSmartLinkDataJsonAsync()
    {
        return await File.ReadAllTextAsync("TestData/SmartLinkResponse.json");
    }

    public static async Task<SmartLinkData> GenerateSmartLinkDataAsync()
    {
        string smartLinkDataJson = await GenerateSmartLinkDataJsonAsync();
        return JsonSerializer.Deserialize<SmartLinkData>(smartLinkDataJson)!;
    }
}