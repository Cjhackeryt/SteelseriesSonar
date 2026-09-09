using System.Globalization;
using System.Text.Json;

namespace SteelSeriesSonarPlugin.Actions;

#nullable disable

public static class ActionParameterExtensions
{
    private static object FindValue(System.Collections.IEnumerable dict, params string[] keys)
    {
        if (dict == null) return null;

        if (dict is IReadOnlyDictionary<string, object> roDict)
        {
            foreach (var key in keys)
            {
                if (roDict.TryGetValue(key, out var val) && val != null)
                    return val;
            }
            foreach (var key in keys)
            {
                foreach (var kvp in roDict)
                {
                    if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
                        return kvp.Value;
                }
            }
        }
        else if (dict is IDictionary<string, object> iDict)
        {
            foreach (var key in keys)
            {
                if (iDict.TryGetValue(key, out var val) && val != null)
                    return val;
            }
            foreach (var key in keys)
            {
                foreach (var kvp in iDict)
                {
                    if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
                        return kvp.Value;
                }
            }
        }

        return null;
    }

    public static string GetString(this IReadOnlyDictionary<string, object> parameters, params string[] keys)
    {
        var val = FindValue(parameters, keys);
        if (val == null) return null;

        if (val is string s) return s;
        if (val is JsonElement elem)
        {
            if (elem.ValueKind == JsonValueKind.String)
                return elem.GetString();
            return elem.GetRawText();
        }
        return val.ToString();
    }

    public static double? GetDouble(this IReadOnlyDictionary<string, object> parameters, params string[] keys)
    {
        var val = FindValue(parameters, keys);
        if (val == null) return null;

        if (val is double d) return d;
        if (val is float f) return f;
        if (val is int i) return i;
        if (val is long l) return l;
        if (val is decimal dec) return (double)dec;
        if (val is JsonElement elem)
        {
            if (elem.ValueKind == JsonValueKind.Number && elem.TryGetDouble(out var jsonD))
                return jsonD;
            if (elem.ValueKind == JsonValueKind.String &&
                double.TryParse(elem.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedStr))
                return parsedStr;
        }
        if (double.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return null;
    }

    public static int? GetInt32(this IReadOnlyDictionary<string, object> parameters, params string[] keys)
    {
        var d = GetDouble(parameters, keys);
        return d.HasValue ? (int)Math.Round(d.Value) : null;
    }

    public static bool? GetBool(this IReadOnlyDictionary<string, object> parameters, params string[] keys)
    {
        var val = FindValue(parameters, keys);
        if (val == null) return null;

        if (val is bool b) return b;
        if (val is JsonElement elem)
        {
            if (elem.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return elem.GetBoolean();
        }
        if (bool.TryParse(val.ToString(), out var parsed))
            return parsed;

        return null;
    }
}
