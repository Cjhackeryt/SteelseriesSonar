using System.Globalization;
using System.Text.Json;
using MacroDeck.Sdk.Actions;

namespace SteelSeriesSonarPlugin.Actions;

/// <summary>
/// Shared action parameter definitions and resilient extraction helpers for Sonar actions.
/// </summary>
internal static class SonarActionParameters
{
    public static ActionParameter Channel(string defaultValue = SonarChannel.Master) =>
        ActionParameter.Choice(
            name: "channel",
            options:
            [
                new ActionParameterOption { Value = SonarChannel.Master, Label = Strings.Common.Channel.Master() },
                new ActionParameterOption { Value = SonarChannel.Game, Label = Strings.Common.Channel.Game() },
                new ActionParameterOption { Value = SonarChannel.ChatRender, Label = Strings.Common.Channel.ChatPlayback() },
                new ActionParameterOption { Value = SonarChannel.ChatCapture, Label = Strings.Common.Channel.ChatMicrophone() },
                new ActionParameterOption { Value = SonarChannel.Media, Label = Strings.Common.Channel.Media() },
                new ActionParameterOption { Value = SonarChannel.Aux, Label = Strings.Common.Channel.Aux() },
                new ActionParameterOption { Value = SonarChannel.Microphone, Label = Strings.Common.Channel.Microphone() },
            ],
            label: Strings.Common.Channel.Label(),
            description: Strings.Common.Channel.Description(),
            defaultValue: defaultValue,
            required: true);

    public static ActionParameter OutputTypeParameter() =>
        ActionParameter.Choice(
            name: "outputType",
            options:
            [
                new ActionParameterOption { Value = "classic", Label = Strings.Common.OutputType.Classic() },
                new ActionParameterOption { Value = "streaming", Label = Strings.Common.OutputType.Streaming() },
                new ActionParameterOption { Value = "monitoring", Label = Strings.Common.OutputType.Monitoring() },
            ],
            label: Strings.Common.OutputType.Label(),
            description: Strings.Common.OutputType.Description(),
            defaultValue: "classic",
            required: true);

#pragma warning disable CS8600, CS8604, CS8620

    public static string ReadChannel(object parametersObj, string fallback = SonarChannel.Master)
    {
        if (parametersObj is IEnumerable<KeyValuePair<string, object>> dict)
        {
            foreach (var kvp in dict)
            {
                if (kvp.Key.Equals("channel", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("ch", StringComparison.OrdinalIgnoreCase))
                {
                    if (kvp.Value != null)
                    {
                        var s = kvp.Value is JsonElement je ? je.ToString() : kvp.Value.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                            return SonarChannel.Normalize(s);
                    }
                }
            }
        }

        return fallback;
    }

    public static OutputType ReadOutputType(object parametersObj)
    {
        if (parametersObj is IEnumerable<KeyValuePair<string, object>> dict)
        {
            foreach (var kvp in dict)
            {
                if (kvp.Key.Equals("outputType", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("output", StringComparison.OrdinalIgnoreCase))
                {
                    if (kvp.Value != null)
                    {
                        var s = (kvp.Value is JsonElement je ? je.ToString() : kvp.Value.ToString())?.Trim().ToLowerInvariant();
                        return s switch
                        {
                            "streaming" => OutputType.Streaming,
                            "monitoring" => OutputType.Monitoring,
                            "classic" => OutputType.Classic,
                            _ => OutputType.Classic
                        };
                    }
                }
            }
        }

        return OutputType.Classic;
    }

    public static double ReadDouble(object parametersObj, string name, double fallback)
    {
        if (parametersObj is IEnumerable<KeyValuePair<string, object>> dict)
        {
            foreach (var kvp in dict)
            {
                if (kvp.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    var raw = kvp.Value;
                    if (raw != null)
                    {
                        if (raw is double d) return d;
                        if (raw is float f) return f;
                        if (raw is int i) return i;
                        if (raw is long l) return l;
                        if (raw is JsonElement je && je.TryGetDouble(out var jd)) return jd;
                        if (double.TryParse(raw.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                            return parsed;
                    }
                }
            }
        }

        return fallback;
    }

    public static string ReadString(object parametersObj, string name, string fallback)
    {
        if (parametersObj is IEnumerable<KeyValuePair<string, object>> dict)
        {
            foreach (var kvp in dict)
            {
                if (kvp.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    var raw = kvp.Value;
                    if (raw != null)
                    {
                        var s = raw is JsonElement je ? je.ToString() : raw.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                            return s;
                    }
                }
            }
        }

        return fallback;
    }

#pragma warning restore CS8600, CS8604, CS8620
}
