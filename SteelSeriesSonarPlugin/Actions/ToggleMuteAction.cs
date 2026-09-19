using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Serilog;

namespace SteelSeriesSonarPlugin.Actions;

/// <summary>
/// Toggles the mute state of a Sonar channel.
/// </summary>
public sealed class ToggleMuteAction : IActionDefinition
{
    private readonly SonarClient _sonar;
    private readonly ILogger _logger;

    public string Id => "toggle-mute";
    public LocalizedText Name => Strings.Actions.ToggleMute.Name();
    public LocalizedText Description => Strings.Actions.ToggleMute.Description();

    public IReadOnlyList<ActionParameter> Parameters { get; } =
    [
        SonarActionParameters.Channel(),
        SonarActionParameters.OutputTypeParameter(),
    ];

    public ToggleMuteAction(SonarClient sonar, ILogger logger)
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

            _logger.Information("ToggleMute: channel={Channel} output={Output}", channel, output);

            try
            {
                var newState = await _sonar.ToggleMuteAsync(channel, output, context.CancellationToken);
                _logger.Information("Channel {Channel} is now {State}", channel, newState ? "muted" : "unmuted");
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to toggle mute on channel {Channel}", channel);
                _sonar.ResetCache();
                return ActionResult.Failed("execution_failed", Strings.Actions.ToggleMute.ExecutionFailed());
            }
        }
    }
}
