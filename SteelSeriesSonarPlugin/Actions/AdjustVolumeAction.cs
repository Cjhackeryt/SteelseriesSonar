using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Microsoft.Extensions.Logging;

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
    public LocalizedText Name => "Adjust Volume";
    public LocalizedText Description => "Increase or decrease a Sonar channel's volume by a step.";

    public IReadOnlyList<ActionParameter> Parameters { get; } =
    [
        SonarActionParameters.Channel(defaultValue: SonarChannel.Game),
        SonarActionParameters.OutputTypeParameter(),
        ActionParameter.Choice(
            name: "direction",
            options:
            [
                new ActionParameterOption { Value = "increase", Label = "Increase" },
                new ActionParameterOption { Value = "decrease", Label = "Decrease" },
            ],
            label: "Direction",
            description: "Increase or decrease the volume.",
            defaultValue: "increase",
            required: true),
        ActionParameter.Slider(
            name: "step",
            min: 1,
            max: 25,
            label: "Step (%)",
            description: "How much to change the volume by (1-25 %).",
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

            _logger.LogInformation("AdjustVolume: channel={Channel} output={Output} step={Step:+0.##;-0.##}", channel, output, step);

            try
            {
                var current = await _sonar.GetVolumeAsync(channel, output, context.CancellationToken);
                var next = Math.Clamp(current + step, 0.0, 1.0);
                await _sonar.SetVolumeAsync(channel, next, output, context.CancellationToken);
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to adjust volume on channel {Channel}", channel);
                _sonar.ResetCache();
                return ActionResult.Failed("execution_failed", ex.Message);
            }
        }
    }
}
