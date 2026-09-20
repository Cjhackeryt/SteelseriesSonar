using System.Collections.Concurrent;
using System.Text.Json;
using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Sdk.Issues;
using MacroDeck.Sdk.Variables;
using Serilog;
using SteelSeriesSonarPlugin.Actions;

namespace SteelSeriesSonarPlugin;

/// <summary>
/// Lifecycle integration for the SteelSeries GG Sonar plugin.
/// Registers actions, surfaces connection issues, and provides real-time variables to Macro Deck.
/// </summary>
public sealed class SonarIntegration : IPluginIntegration, IIntegrationIssueProvider, IVariableProvider
{
    private const string IssueIdSonarUnavailable = "sonar-unavailable";

    private readonly SonarClient _sonar;
    private readonly ILogger _logger;

    public IReadOnlyList<IActionDefinition> Actions { get; }

    public IReadOnlyList<VariableDefinition> Variables => VariablesList;
    public IReadOnlyList<VariableDefinition> DeclaredVariables => VariablesList;
    public bool VariablesDependOnConfiguration => false;

    private static readonly TimeSpan FastRefresh = TimeSpan.FromMilliseconds(200);

    // ── Authoritative variable declarations ──────────────────────────────────

    // ── Authoritative channel ↔ variable mapping (single source of truth) ────

    /// <summary>Volume variable name → Sonar API channel identifier.</summary>
    private static readonly Dictionary<string, (string Channel, OutputType Output)> VolumeVarToTarget = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sonar_master_volume"] = (SonarChannel.Master, OutputType.Classic),
        ["sonar_game_volume"] = (SonarChannel.Game, OutputType.Classic),
        ["sonar_chat_volume"] = (SonarChannel.ChatRender, OutputType.Classic),
        ["sonar_media_volume"] = (SonarChannel.Media, OutputType.Classic),
        ["sonar_aux_volume"] = (SonarChannel.Aux, OutputType.Classic),
        ["sonar_mic_volume"] = (SonarChannel.ChatCapture, OutputType.Classic),
    };

    private static readonly Dictionary<string, (string Channel, OutputType Output)> OutputVolumeVarToTarget =
        CreateOutputVolumeTargets();

    /// <summary>Mute variable name → Sonar API channel identifier.</summary>
    private static readonly Dictionary<string, string> MuteVarToChannel = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sonar_master_muted"] = "master",
        ["sonar_game_muted"]   = "game",
        ["sonar_chat_muted"]   = "chatRender",
        ["sonar_media_muted"]  = "media",
        ["sonar_aux_muted"]    = "aux",
        ["sonar_mic_muted"]    = "chatCapture",
    };

    private static readonly IReadOnlyList<VariableDefinition> VariablesList = CreateVariables();

    // ── Last-known-good value cache ──────────────────────────────────────────
    // Prevents transient API failures from resetting the UI to 0/null.

    private readonly ConcurrentDictionary<string, object?> _lastKnownValues = new(StringComparer.OrdinalIgnoreCase);

    public SonarIntegration(SonarClient sonar, ILogger logger)
    {
        _sonar = sonar;
        _logger = logger;

        foreach (var key in CreateOutputVolumeTargets().Keys)
            _lastKnownValues[key] = 0.0;

        foreach (var key in MuteVarToChannel.Keys)
            _lastKnownValues[key] = false;

        _lastKnownValues["sonar_chatmix"] = 0.0;

        Actions =
        [
            new SetVolumeAction(_sonar, _logger),
            new AdjustVolumeAction(_sonar, _logger),
            new MuteAction(_sonar, _logger),
            new ToggleMuteAction(_sonar, _logger),
            new SetChatMixAction(_sonar, _logger),
            new SetStreamerModeAction(_sonar, _logger)
        ];
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task InitializeAsync(IIntegrationContext context)
    {
        _logger.Information("[Sonar] Integration initializing…");

        var available = await _sonar.IsAvailableAsync();
        if (available)
        {
            _logger.Information("[Sonar] Sonar is reachable — reading initial state.");
            try
            {
                foreach (var (name, target) in VolumeVarToTarget)
                {
                    var raw = await _sonar.GetVolumeAsync(target.Channel, target.Output);
                    _lastKnownValues[name] = ClampVolume(raw);
                }

                foreach (var (name, muteChannel) in MuteVarToChannel)
                {
                    var muted = await _sonar.GetMuteAsync(muteChannel, OutputType.None);
                    _lastKnownValues[name] = muted;
                }

                var chatmix = await _sonar.GetChatMixAsync();
                _lastKnownValues["sonar_chatmix"] = ClampChatMix(chatmix);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "[Sonar] Failed to pre-fetch initial state from Sonar");
            }
        }
        else
        {
            _logger.Warning(
                "[Sonar] Sonar is not currently reachable. " +
                "Variables will populate once SteelSeries GG and Sonar are launched.");
        }
    }

    /// <inheritdoc />
    public Task ShutdownAsync()
    {
        _logger.Information("[Sonar] Integration shutting down.");
        return Task.CompletedTask;
    }

    // ── Variable provider ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<VariableReading> ReadAsync(string name, CancellationToken cancellationToken = default)
    {
        var lowerName = NormalizeVariableName(name);

        try
        {
            object? freshValue = null;

            if (VolumeVarToTarget.TryGetValue(lowerName, out var volChannel) ||
                OutputVolumeVarToTarget.TryGetValue(lowerName, out volChannel))
            {
                var raw = await _sonar.GetVolumeAsync(volChannel.Channel, volChannel.Output, cancellationToken);
                freshValue = ClampVolume(raw);
                _logger.Debug("[Sonar] {Channel} volume read: {Value}", volChannel, freshValue);
            }
            else if (MuteVarToChannel.TryGetValue(lowerName, out var muteChannel))
            {
                // Unsuffixed mute variables follow the currently active Sonar
                // output, just like the legacy actions do.
                freshValue = await _sonar.GetMuteAsync(muteChannel, OutputType.None, cancellationToken);
                _logger.Debug("[Sonar] {Channel} muted read: {Value}", muteChannel, freshValue);
            }
            else if (lowerName == "sonar_chatmix")
            {
                var raw = await _sonar.GetChatMixAsync(cancellationToken);
                freshValue = ClampChatMix(raw);
                _logger.Debug("[Sonar] ChatMix read: {Value}", freshValue);
            }

            if (freshValue is not null)
            {
                _lastKnownValues[lowerName] = freshValue;
                return VariableReading.Of(freshValue);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "[Sonar] Failed to read variable '{Name}' — returning last-known value", lowerName);

            // On failure after a successful connection loss, reset cache so
            // re-discovery happens on the next attempt.
            _sonar.ResetCache();
        }

        if (_lastKnownValues.TryGetValue(lowerName, out var cached))
            return VariableReading.Of(cached);

        if (MuteVarToChannel.ContainsKey(lowerName))
            return VariableReading.Of(false);

        return VariableReading.Of(0.0);
    }

    public async ValueTask<VariableWriteResult> SetValueAsync(
        string name, object? value, CancellationToken cancellationToken = default)
    {
        var lowerName = NormalizeVariableName(name);
        if (MuteVarToChannel.TryGetValue(lowerName, out var muteChannel))
        {
            if (!TryReadBoolean(value, out var muted))
                return VariableWriteResult.InvalidValue();

            _lastKnownValues[lowerName] = muted;
            try
            {
                await _sonar.SetMuteAsync(muteChannel, muted, OutputType.None, cancellationToken);
                return VariableWriteResult.Applied();
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "[Sonar] Failed to write mute variable '{Name}' to Sonar - updated cached state", lowerName);
                _sonar.ResetCache();
                return VariableWriteResult.Applied();
            }
        }

        if (!VolumeVarToTarget.TryGetValue(lowerName, out var target) &&
            !OutputVolumeVarToTarget.TryGetValue(lowerName, out target))
            return VariableWriteResult.NotWritable();

        if (!TryReadNumeric(value, out var percent) || double.IsNaN(percent) || double.IsInfinity(percent))
            return VariableWriteResult.InvalidValue();

        var clamped = Math.Clamp(percent, 0, 100);
        _lastKnownValues[lowerName] = Math.Round(clamped);
        try
        {
            await _sonar.SetVolumeAsync(target.Channel, clamped / 100.0, target.Output, cancellationToken);
            return VariableWriteResult.Applied();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "[Sonar] Failed to write volume variable '{Name}' to Sonar - updated cached state", lowerName);
            _sonar.ResetCache();
            return VariableWriteResult.Applied();
        }
    }

    private static string NormalizeVariableName(string name)
    {
        var normalized = name.Trim().Replace('-', '_').ToLowerInvariant();
        if (normalized.EndsWith("_mute"))
            normalized += "d";
        return normalized;
    }

    private static IReadOnlyList<VariableDefinition> CreateVariables()
    {
        var variables = new List<VariableDefinition>();
        foreach (var name in CreateOutputVolumeTargets().Keys)
        {
            variables.Add(VariableDefinition.Eager(name, VariableType.Numeric, 0, FastRefresh) with
            {
                Id = ToLocalId(name),
                DisplayName = FormatDisplayName(name),
                Unit = "%",
                SemanticKind = VariableSemanticKinds.Percentage,
                Write = new VariableWriteCapability { CommitOnRelease = false }
            });
        }

        foreach (var name in MuteVarToChannel.Keys)
        {
            variables.Add(VariableDefinition.Eager(name, VariableType.Boolean, refreshInterval: FastRefresh) with
            {
                Id = ToLocalId(name),
                DisplayName = FormatDisplayName(name),
                Write = new VariableWriteCapability { CommitOnRelease = false }
            });
        }

        variables.Add(VariableDefinition.Eager("sonar_chatmix", VariableType.Numeric, 0, FastRefresh) with
        {
            Id = ToLocalId("sonar_chatmix"),
            DisplayName = FormatDisplayName("sonar_chatmix"),
            Unit = "%",
            SemanticKind = VariableSemanticKinds.Percentage
        });
        return variables;
    }

    private static string FormatDisplayName(string name)
    {
        if (string.Equals(name, "sonar_chatmix", StringComparison.OrdinalIgnoreCase))
            return "Chat Mix";

        var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var words = new List<string>();
        for (var i = 1; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Equals("streaming", StringComparison.OrdinalIgnoreCase))
                words.Add("(Streaming)");
            else if (p.Equals("monitoring", StringComparison.OrdinalIgnoreCase))
                words.Add("(Monitoring)");
            else if (p.Length > 0)
                words.Add(char.ToUpperInvariant(p[0]) + p[1..]);
        }
        return string.Join(" ", words);
    }

    private static Dictionary<string, (string Channel, OutputType Output)> CreateOutputVolumeTargets()
    {
        var targets = new Dictionary<string, (string, OutputType)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, channel) in new[]
        {
            ("master", SonarChannel.Master),
            ("game", SonarChannel.Game),
            ("chat", SonarChannel.ChatRender),
            ("media", SonarChannel.Media),
            ("aux", SonarChannel.Aux),
            ("mic", SonarChannel.ChatCapture),
        })
        {
            targets[$"sonar_{name}_volume"] = (channel, OutputType.Classic);
            targets[$"sonar_{name}_volume_streaming"] = (channel, OutputType.Streaming);
            targets[$"sonar_{name}_volume_monitoring"] = (channel, OutputType.Monitoring);
        }
        return targets;
    }

    private static string ToLocalId(string name) =>
        name.Replace('_', '-');

    private static bool TryReadNumeric(object? value, out double number)
    {
        number = 0;
        if (value is JsonElement element)
            return element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out number);
        if (value is IConvertible convertible)
        {
            try
            {
                number = convertible.ToDouble(System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            catch (FormatException) { }
            catch (InvalidCastException) { }
        }
        return false;
    }

    private static bool TryReadBoolean(object? value, out bool state)
    {
        if (value is bool boolean)
        {
            state = boolean;
            return true;
        }

        if (value is JsonElement element)
        {
            if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                state = element.GetBoolean();
                return true;
            }

            if (element.ValueKind == JsonValueKind.String &&
                bool.TryParse(element.GetString(), out state))
                return true;
        }

        if (value is string text && bool.TryParse(text, out state))
            return true;

        state = false;
        return false;
    }

    // ── Safe value converters ─────────────────────────────────────────────────

    /// <summary>
    /// Converts a raw Sonar volume (0.0–1.0) to a clamped percentage (0–100).
    /// Safely handles NaN, Infinity, and out-of-range values.
    /// </summary>
    private static double ClampVolume(double rawApiValue)
    {
        var percent = Math.Round(rawApiValue * 100.0);
        if (double.IsNaN(percent) || double.IsInfinity(percent))
            return 0;
        return Math.Clamp(percent, 0, 100);
    }

    /// <summary>
    /// Converts a raw Sonar chat-mix value (−1.0 to +1.0) to a clamped percentage (−100 to +100).
    /// </summary>
    private static double ClampChatMix(double rawApiValue)
    {
        var percent = Math.Round(rawApiValue * 100.0);
        if (double.IsNaN(percent) || double.IsInfinity(percent))
            return 0;
        return Math.Clamp(percent, -100, 100);
    }

    // ── Issue provider ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<IntegrationIssue>> GetIssuesAsync(CancellationToken cancellationToken)
    {
        var available = await _sonar.IsAvailableAsync(cancellationToken);
        if (available)
        {
            return [];
        }

        return
        [
            new IntegrationIssue
            {
                Id = IssueIdSonarUnavailable,
                Title = Strings.Common.Issue.SonarUnavailable.Title(),
                Description = Strings.Common.Issue.SonarUnavailable.Description(),
                Severity = IntegrationIssueSeverity.Warning,
            }
        ];
    }

    /// <inheritdoc />
    public Task<IssueResolution> ResolveIssueAsync(string issueId, CancellationToken cancellationToken) =>
        Task.FromResult(IssueResolution.Failed(Strings.Common.Issue.SonarUnavailable.Resolve()));
}
