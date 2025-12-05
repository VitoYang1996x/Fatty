using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fatty.Services;
using Lavalink4NET.Extensions;

var builder = Host.CreateApplicationBuilder(args);

var envLavalinkUri = Environment.GetEnvironmentVariable("Lavalink__BaseAddress") ?? "http://localhost:2333";
var envLavalinkPass = Environment.GetEnvironmentVariable("Lavalink__Passphrase") ?? "youshallnotpass";

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


builder.Services.AddLavalink()
    .ConfigureLavalink(options =>
    {
        options.BaseAddress = new Uri(envLavalinkUri);
        options.Passphrase = envLavalinkPass;
    });

builder.Services.AddHostedService<BotHostingService>();

var host = builder.Build();
await host.RunAsync();
