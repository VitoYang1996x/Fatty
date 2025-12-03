using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using System.Runtime.InteropServices;

namespace Fatty
{
    public class BotHostingService : BackgroundService
    {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly InteractionService _interactionService;
        private readonly IAudioService _audioService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BotHostingService> _logger;

        public BotHostingService(
        DiscordSocketClient discordSocketClient,
        InteractionService interactionService,
        IAudioService audioService,
        IConfiguration configuration,
        ILogger<BotHostingService> logger)
        {
            _discordSocketClient = discordSocketClient;
            _interactionService = interactionService;
            _audioService = audioService;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //step1. read token from appsetting
            var token = _configuration["DiscordToken"];
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError($"Error¡GCould not find 'DiscordToken' in appsetting !");
                return;
            }


            //step2. subscribe log event
            _discordSocketClient.Log += LogAsync;
            _interactionService.Log += LogAsync;

            //steo3. processing "ready" event
            //lavalink4net requires the bot to be connected before it can be used
            var clientReady = new TaskCompletionSource();
            _discordSocketClient.Ready += async () =>
            {
                _logger.LogInformation("Discord Bot Connected,Start Registring Command...");

                // await _interactionService.RegisterCommandsGloballyAsync();

                clientReady.TrySetResult();
            };

            //step4. login and start discord
            await _discordSocketClient.LoginAsync(TokenType.Bot, token);
            await _discordSocketClient.StartAsync();

            //step5. wait for discord client ready
            await clientReady.Task;

            //step6. start lavalink audio service
            _logger.LogInformation("Discord Bot Launching Successfully ! Lavalink Service is running background now...");

            //step7. keep the program running until a stop signal is received (e.g., you pressed Ctrl+C)
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }



        /// <summary>
        /// Simple Logging Support For Development
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        private Task LogAsync(LogMessage log)
        {
            _logger.LogInformation(log.ToString());
            return Task.CompletedTask;
        }
    }
}
