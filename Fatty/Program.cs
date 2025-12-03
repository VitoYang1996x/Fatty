using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Fatty;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<DiscordSocketClient>(sp =>
{
    var config = new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMessages | GatewayIntents.GuildVoiceStates,
        LogLevel = LogSeverity.Info,
        AlwaysDownloadUsers = false
    };
    return new DiscordSocketClient(config);
});

builder.Services.AddSingleton<InteractionService>(sp =>
{
    var client = sp.GetRequiredService<DiscordSocketClient>();
    return new InteractionService(client);
});

builder.Services.AddLavalink();

builder.Services.AddHostedService<BotHostingService>();

var host = builder.Build();
await host.RunAsync();
