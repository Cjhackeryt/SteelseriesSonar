using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Serilog;

namespace SteelSeriesSonarPlugin.Actions;

/// <summary>
/// Nudges a Sonar channel's volume up or down by a configurable step. Supports Classic mode
/// and Streamer Mode (Streaming / Monitoring outputs).
/// </summary>
public sealed class AdjustVolumeAction : IActionDefinition
{
    private readonly SonarClient _sonar;
    private readonly ILogger _logger;

    public string Id => "adjust-volume";
    public LocalizedText Name => Strings.Actions.AdjustVolume.Name();
    public LocalizedText Description => Strings.Actions.AdjustVolume.Description();

    public IReadOnlyList<ActionParameter> Parameters { get; } =
    [
        SonarActionParameters.Channel(defaultValue: SonarChannel.Game),
        SonarActionParameters.OutputTypeParameter(),
        ActionParameter.Choice(
            name: "direction",
            options:
            [
                new ActionParameterOption { Value = "increase", Label = Strings.Common.Action.Increase() },
                new ActionParameterOption { Value = "decrease", Label = Strings.Common.Action.Decrease() },
            ],
            label: Strings.Actions.AdjustVolume.Parameters.Direction.Label(),
            description: Strings.Actions.AdjustVolume.Parameters.Direction.Description(),
            defaultValue: "increase",
            required: true),
        ActionParameter.Slider(
            name: "step",
            min: 1,
            max: 25,
            label: Strings.Actions.AdjustVolume.Parameters.Step.Label(),
            description: Strings.Actions.AdjustVolume.Parameters.Step.Description(),
            step: 1,
            defaultValue: 5),
    ];

    public AdjustVolumeAction(SonarClient sonar, ILogger logger)
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
            var channel = SonarActionParameters.ReadChannel(context.Parameters, fallback: SonarChannel.Game);
            var output = SonarActionParameters.ReadOutputType(context.Parameters);
            var direction = SonarActionParameters.ReadString(context.Parameters, "direction", "increase");
            var decrease = direction.Equals("decrease", StringComparison.OrdinalIgnoreCase);
            var stepPercent = SonarActionParameters.ReadDouble(context.Parameters, "step", 5.0);
            var step = decrease ? -(stepPercent / 100.0) : stepPercent / 100.0;

            _logger.Information("AdjustVolume: channel={Channel} output={Output} step={Step:+0.##;-0.##}", channel, output, step);

            try
            {
                var current = await _sonar.GetVolumeAsync(channel, output, context.CancellationToken);
                var next = Math.Clamp(current + step, 0.0, 1.0);
                await _sonar.SetVolumeAsync(channel, next, output, context.CancellationToken);
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to adjust volume on channel {Channel}", channel);
                _sonar.ResetCache();
                return ActionResult.Failed("execution_failed", Strings.Actions.AdjustVolume.ExecutionFailed());
            }
        }
    }
}
