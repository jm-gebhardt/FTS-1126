using Newtonsoft.Json.Linq;

namespace Gebhardt.Acc.IntegrationTest.Extensions;

public static class JObjectExtensions
{
    private static Func<JProperty, bool> FindPropertyByName(string propertyName)
    {
        return p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase);
    }

    public static string? GetStringValue(this JObject jObject, string propertyName)
    {
        var prop = jObject.Properties()
            .SingleOrDefault(FindPropertyByName(propertyName));

        return prop?.Value.ToString();
    }

    public static JArray? GetArray(this JObject jObject, string propertyName)
    {
        var prop = jObject.Properties()
            .SingleOrDefault(FindPropertyByName(propertyName));

        return prop?.Value as JArray;
    }

    public static void TryOverwriteValue(this JObject jObject, string propertyName, object? newValue)
    {
        var parts = propertyName.Split('.');

        JToken current = jObject;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (current is JObject obj)
            {
                var next = obj.Properties()
                    .FirstOrDefault(p => FindPropertyByName(parts[i])(p));

                if (next == null || next.Value is not JObject)
                {
                    return;
                }

                current = next.Value;
            }
            else
            {
                return;
            }
        }

        if (current is JObject lastObj)
        {
            var lastProp = lastObj.Properties()
                .FirstOrDefault(p => FindPropertyByName(parts[^1])(p));


            if (lastProp != null)
            {
                lastProp.Value = newValue != null ? new JValue(newValue) : JValue.CreateNull();
            }
        }
    }

    public static void TrySetArray(this JObject jObject, string propertyName, JArray array)
    {
        var prop = jObject.Properties()
            .SingleOrDefault(FindPropertyByName(propertyName));

        if (prop != null)
        {
            prop.Value = array;
        }
        else
        {
            jObject[propertyName] = array;
        }
    }
}