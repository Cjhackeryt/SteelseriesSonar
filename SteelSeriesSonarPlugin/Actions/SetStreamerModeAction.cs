using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Microsoft.Extensions.Logging;

namespace SteelSeriesSonarPlugin.Actions;

/// <summary>
/// Enables, disables, or toggles SteelSeries GG Sonar Streamer Mode.
/// </summary>
public sealed class SetStreamerModeAction : IActionDefinition
{
    private readonly SonarClient _sonar;
    private readonly ILogger _logger;

    public string Id => "set-streamer-mode";
    public LocalizedText Name => "Set Streamer Mode";
    public LocalizedText Description => "Enable, disable, or toggle Sonar Streamer Mode (dual Streaming/Monitoring sliders).";

    public IReadOnlyList<ActionParameter> Parameters { get; } =
    [
        ActionParameter.Choice(
            name: "action",
            options:
            [
                new ActionParameterOption { Value = "enable", Label = "Enable" },
                new ActionParameterOption { Value = "disable", Label = "Disable" },
                new ActionParameterOption { Value = "toggle", Label = "Toggle" },
            ],
            label: "Action",
            description: "Enable / Disable Streamer Mode, or Toggle (flip) the current state.",
            defaultValue: "toggle",
            required: true),
    ];

    public SetStreamerModeAction(SonarClient sonar, ILogger logger)
    {
        _sonar = sonar;
        _logger = logger;
    }

    public IActionExecutor CreateExecutor() => new Executor(_sonar, _logger);

    private sealed class Executor : IActionExecutor
    {
        private readonly SonarClient _sonar;
        private readonly ILogger _logger;

        public Executor(SonarClient sonar, ILogger logger)
        {
            _sonar = sonar;
            _logger = logger;
        }

        public async Task<ActionResult> ExecuteAsync(ActionExecutionContext context)
        {
            var action = SonarActionParameters.ReadString(context.Parameters, "action", "toggle").ToLowerInvariant();

            _logger.LogInformation("SetStreamerMode: action={Action}", action);

            try
            {
                var newState = action switch
                {
                    "enable" => await EnableAsync(context.CancellationToken),
                    "disable" => await DisableAsync(context.CancellationToken),
                    _ => await _sonar.ToggleStreamerModeAsync(context.CancellationToken)
                };

                _logger.LogInformation("Streamer Mode is now {State}.", newState ? "enabled" : "disabled");
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change Streamer Mode");
                _sonar.ResetCache();
                return ActionResult.Failed("execution_failed", ex.Message);
            }
        }

        private async Task<bool> EnableAsync(CancellationToken ct)
        {
            await _sonar.SetStreamerModeAsync(true, ct);
            return true;
        }

        private async Task<bool> DisableAsync(CancellationToken ct)
        {
            await _sonar.SetStreamerModeAsync(false, ct);
            return false;
        }
    }
}
