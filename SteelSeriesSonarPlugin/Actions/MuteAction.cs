using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Serilog;

namespace SteelSeriesSonarPlugin.Actions;

/// <summary>
/// Explicitly mutes or unmutes a Sonar channel.
/// </summary>
public sealed class MuteAction : IActionDefinition
{
    private readonly SonarClient _sonar;
    private readonly ILogger _logger;

    public string Id => "mute";
    public LocalizedText Name => Strings.Actions.Mute.Name();
    public LocalizedText Description => Strings.Actions.Mute.Description();

    public IReadOnlyList<ActionParameter> Parameters { get; } =
    [
        SonarActionParameters.Channel(),
        SonarActionParameters.OutputTypeParameter(),
        ActionParameter.Choice(
            name: "muteState",
            options:
            [
                new ActionParameterOption { Value = "mute", Label = Strings.Common.Action.Mute() },
                new ActionParameterOption { Value = "unmute", Label = Strings.Common.Action.Unmute() },
            ],
            label: Strings.Actions.Mute.Parameters.MuteState.Label(),
            description: Strings.Actions.Mute.Parameters.MuteState.Description(),
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

            _logger.Information("Mute: channel={Channel} output={Output} mute={Mute}", channel, output, mute);

            try
            {
                await _sonar.SetMuteAsync(channel, mute, output, context.CancellationToken);
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to set mute on channel {Channel}", channel);
                _sonar.ResetCache();
                return ActionResult.Failed("execution_failed", Strings.Actions.Mute.ExecutionFailed());
            }
        }
    }
}
