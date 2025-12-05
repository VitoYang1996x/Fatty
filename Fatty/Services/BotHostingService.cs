using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Fatty.Services;

public class BotHostingService : BackgroundService
{
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAudioService _audioService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BotHostingService> _logger;

    public BotHostingService(
    DiscordSocketClient discordSocketClient,
    InteractionService interactionService,
    IServiceProvider serviceProvider,
    IAudioService audioService,
    IConfiguration configuration,
    ILogger<BotHostingService> logger)
    {
        _discordSocketClient = discordSocketClient;
        _interactionService = interactionService;
        _serviceProvider = serviceProvider;
        _audioService = audioService;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //read token from appsetting
        var token = _configuration["DiscordToken"];
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError($"Error¡GCould not find 'DiscordToken' in appsetting !");
            return;
        }


        //subscribe log event
        _discordSocketClient.Log += LogAsync;
        _interactionService.Log += LogAsync;

        _discordSocketClient.InteractionCreated += OnInteractionCreated;

        //processing "ready" event
        //lavalink4net requires the bot to be connected before it can be used
        var clientReady = new TaskCompletionSource();
        _discordSocketClient.Ready += async () =>
        {
            _logger.LogInformation("Discord Bot Connected,Start Registring Command...");

            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

            //register interaction commands to guild or globally
            //global registering (RegisterCommandsGloballyAsync) might affect up to an hour to take effect
            //recommand to use RegisterCommandsToGuildAsync(guildId) in dev stage,it will take effect instantly
            await _interactionService.RegisterCommandsGloballyAsync();
            //await _interactionService.RegisterCommandsToGuildAsync(647680254726635550);

            clientReady.TrySetResult();
        };

        //login and start discord
        await _discordSocketClient.LoginAsync(TokenType.Bot, token);
        await _discordSocketClient.StartAsync();

        //wait for discord client ready
        await clientReady.Task;

        //start lavalink audio service
        _logger.LogInformation("Discord Bot Launching Successfully ! Lavalink Service is running background now...");

        //keep the program running until a stop signal is received (e.g., you pressed Ctrl+C)
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnInteractionCreated(SocketInteraction interaction)
    {
        _logger.LogInformation($"[RecvCommand] Sent From {interaction.User.Username}");
        try
        {
            var ctx = new SocketInteractionContext(_discordSocketClient, interaction);
            await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "command executing failed.");
            // If a Slash Command execution fails it is most likely that the original interaction acknowledgement
            if (interaction.Type == InteractionType.ApplicationCommand)
            {
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }

    /// <summary>
    /// Simple Logging Support For Development
    /// </summary>
    /// <param name="log"></param>
    /// <returns></returns>
    private Task LogAsync(LogMessage log)
    {
        _logger.LogInformation($"[System] {log.ToString()}");
        return Task.CompletedTask;
    }
}
