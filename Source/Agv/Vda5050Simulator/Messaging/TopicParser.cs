using Gebhardt.Acc.Vda5050Simulator.Domain;

namespace Gebhardt.Acc.Vda5050Simulator.Messaging;

public static class TopicParser
{
    public static string BuildTopic(string interfaceName, string version, AgvIdentity id, string subtopic)
        => $"{interfaceName}/{version}/{id.Manufacturer}/{id.SerialNumber}/{subtopic}";

    public static bool TryParse(string topic, out AgvIdentity? id, out string subtopic)
    {
        string[] parts = topic.Split('/');
        if (parts.Length == 5)
        {
            id = new AgvIdentity(parts[2], string.Empty, parts[3]);
            subtopic = parts[4];
            return true;
        }
        id = null;
        subtopic = string.Empty;
        return false;
    }
}
