using System.Text.Json;
using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunicationTest.Helpers;

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