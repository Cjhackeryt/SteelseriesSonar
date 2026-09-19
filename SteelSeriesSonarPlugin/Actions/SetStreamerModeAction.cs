using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Serilog;

namespace SteelSeriesSonarPlugin.Actions;

/// <summary>
/// Enables, disables, or toggles SteelSeries GG Sonar Streamer Mode.
/// </summary>
public sealed class SetStreamerModeAction : IActionDefinition
{
    private readonly SonarClient _sonar;
    private readonly ILogger _logger;

    public string Id => "set-streamer-mode";
    public LocalizedText Name => Strings.Actions.SetStreamerMode.Name();
    public LocalizedText Description => Strings.Actions.SetStreamerMode.Description();

    public IReadOnlyList<ActionParameter> Parameters { get; } =
    [
        ActionParameter.Choice(
            name: "action",
            options:
            [
                new ActionParameterOption { Value = "enable", Label = Strings.Common.Action.Enable() },
                new ActionParameterOption { Value = "disable", Label = Strings.Common.Action.Disable() },
                new ActionParameterOption { Value = "toggle", Label = Strings.Common.Action.Toggle() },
            ],
            label: Strings.Actions.SetStreamerMode.Parameters.Action.Label(),
            description: Strings.Actions.SetStreamerMode.Parameters.Action.Description(),
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

            _logger.Information("SetStreamerMode: action={Action}", action);

            try
            {
                var newState = action switch
                {
                    "enable" => await EnableAsync(context.CancellationToken),
                    "disable" => await DisableAsync(context.CancellationToken),
                    _ => await _sonar.ToggleStreamerModeAsync(context.CancellationToken)
                };

                _logger.Information("Streamer Mode is now {State}.", newState ? "enabled" : "disabled");
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to change Streamer Mode");
                _sonar.ResetCache();
                return ActionResult.Failed("execution_failed", Strings.Actions.SetStreamerMode.ExecutionFailed());
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
