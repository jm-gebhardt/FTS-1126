using Gebhardt.Acc.Vda5050Simulator.Domain;

namespace Gebhardt.Acc.Vda5050Simulator.Messaging;

public static class TopicParser
{
    public static string BuildTopic(string interfaceName, string version, AgvId id, string subtopic)
        => $"{interfaceName}/{version}/{id.Manufacturer}/{id.SerialNumber}/{subtopic}";

    public static bool TryParse(string topic, out AgvId id, out string subtopic)
    {
        string[] parts = topic.Split('/');
        if (parts.Length == 5)
        {
            id = new AgvId(parts[2], parts[3]);
            subtopic = parts[4];
            return true;
        }
        id = default;
        subtopic = string.Empty;
        return false;
    }
}
