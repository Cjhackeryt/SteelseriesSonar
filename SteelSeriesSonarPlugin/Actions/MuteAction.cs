using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Microsoft.Extensions.Logging;

namespace SteelSeriesSonarPlugin.Actions;

/// <summary>
/// Explicitly mutes or unmutes a Sonar channel.
/// </summary>
public sealed class MuteAction : IActionDefinition
{
    private readonly SonarClient _sonar;
    private readonly ILogger _logger;

    public string Id => "mute";
    public LocalizedText Name => "Mute / Unmute";
    public LocalizedText Description => "Set the mute state of a Sonar channel to On or Off.";

    public IReadOnlyList<ActionParameter> Parameters { get; } =
    [
        SonarActionParameters.Channel(),
        SonarActionParameters.OutputTypeParameter(),
        ActionParameter.Choice(
            name: "muteState",
            options:
            [
                new ActionParameterOption { Value = "mute", Label = "Mute" },
                new ActionParameterOption { Value = "unmute", Label = "Unmute" },
            ],
            label: "State",
            description: "Mute or unmute the channel.",
            defaultValue: "mute",
            required: true),
    ];

    public MuteAction(SonarClient sonar, ILogger logger)
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
            var channel = SonarActionParameters.ReadChannel(context.Parameters);
            var output = SonarActionParameters.ReadOutputType(context.Parameters);
            var state = SonarActionParameters.ReadString(context.Parameters, "muteState", "mute");
            var mute = state.Equals("mute", StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation("Mute: channel={Channel} output={Output} mute={Mute}", channel, output, mute);

            try
            {
                await _sonar.SetMuteAsync(channel, mute, output, context.CancellationToken);
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set mute on channel {Channel}", channel);
                _sonar.ResetCache();
                return ActionResult.Failed("execution_failed", ex.Message);
            }
        }
    }
}
