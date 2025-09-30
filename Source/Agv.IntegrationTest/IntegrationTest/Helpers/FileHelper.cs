using Newtonsoft.Json.Linq;

namespace Gebhardt.Acc.IntegrationTest.Helpers;

public static class FileHelper
{
    /// <summary>
    /// Read JSON file.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="fileName"></param>
    /// <returns>parsed json object</returns>
    public static JObject ReadJson(string directory, string fileName)
    {
        var filePath = $"{directory}/{fileName}";
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Could not find MQTT sample file: '{fileName}'");
        }

        return JObject.Parse(File.ReadAllText(filePath));
    }
}