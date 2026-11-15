using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SharpFAI.Serialization;

/// <summary>
/// Converts the loosely typed tree that <see cref="SimpleJSON"/> produces into Newtonsoft tokens.
/// Going through text instead would re-serialise the whole document and parse it again, which on a
/// 40 MB chart costs seconds on its own.
/// </summary>
public static class JsonValueConverter
{
    /// <summary>Convert a parsed level dictionary into a <see cref="JObject"/>.</summary>
    public static JObject ToJObject(IDictionary<string, object> source)
    {
        JObject result = [];

        foreach (KeyValuePair<string, object> pair in source)
        {
            // Add skips the indexer's duplicate scan, which is what makes a large chart quadratic.
            result.Add(pair.Key, ToToken(pair.Value));
        }

        return result;
    }

    /// <summary>Convert one parsed value into the matching token.</summary>
    public static JToken ToToken(object? value) => value switch
    {
        null => JValue.CreateNull(),
        JToken token => token,
        string text => new JValue(text),
        bool flag => new JValue(flag),
        int number => new JValue(number),
        float number => new JValue(number),
        double number => new JValue(number),
        long number => new JValue(number),
        // SimpleJSON hands back the concrete collection types; matching them directly avoids two
        // interface dispatches per value.
        Dictionary<string, object> map => ToJObject(map),
        List<object> list => ToArray(list),
        IDictionary<string, object> map => ToJObject(map),
        IList list => ToArray(list),
        _ => ToTokenFromObject(value),
    };

    /// <summary>Fallback conversion for arbitrary POCOs, used by <c>BaseEvent.ToEvent</c>.</summary>
    private static JToken ToTokenFromObject(object value) => JToken.FromObject(value);

    private static JArray ToArray(IList source)
    {
        JArray result = [];

        foreach (object? item in source)
        {
            result.Add(ToToken(item));
        }

        return result;
    }
}
