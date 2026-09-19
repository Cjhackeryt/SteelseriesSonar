using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Serilog;

namespace SteelSeriesSonarPlugin.Actions;

/// <summary>
/// Sets the Sonar Chat Mix balance slider. -100 = full chat audio, 0 = balanced,
/// +100 = full game audio.
/// </summary>
public sealed class SetChatMixAction : IActionDefinition
{
    private readonly SonarClient _sonar;
    private readonly ILogger _logger;

    public string Id => "set-chat-mix";
    public LocalizedText Name => Strings.Actions.SetChatMix.Name();
    public LocalizedText Description => Strings.Actions.SetChatMix.Description();

    public IReadOnlyList<ActionParameter> Parameters { get; } =
    [
        ActionParameter.Slider(
            name: "chatMix",
            min: -100,
            max: 100,
            label: Strings.Actions.SetChatMix.Parameters.ChatMix.Label(),
            description: Strings.Actions.SetChatMix.Parameters.ChatMix.Description(),
            step: 1,
            defaultValue: 0),
    ];

    public SetChatMixAction(SonarClient sonar, ILogger logger)
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
            var chatMixValue = SonarActionParameters.ReadDouble(context.Parameters, "chatMix", 0.0);
            var chatMix = Math.Clamp(chatMixValue / 100.0, -1.0, 1.0);

            _logger.Information("SetChatMix: value={Value}", chatMix);

            try
            {
                await _sonar.SetChatMixAsync(chatMix, context.CancellationToken);
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to set chat mix");
                _sonar.ResetCache();
                return ActionResult.Failed("execution_failed", Strings.Actions.SetChatMix.ExecutionFailed());
            }
        }
    }
}
