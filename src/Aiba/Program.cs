using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace AcidChicken.Aiba
{
    public static class Program
    {
        internal const string ConfigPath = "config.json";

        internal const string Endpoint = "https://api.coinmarketcap.com/v1/ticker/?convert=JPY&limit=0";

        internal const string Prefix = ";";

        internal const string SplashArt = "                ,,  ,,                 \n      db        db *MM                 \n     ;MM:           MM                 \n    ,V^MM.    `7MM  MM,dMMb.   ,6\"Yb.  \n   ,M  `MM      MM  MM    `Mb 8)   MM  \n   AbmmmqMA     MM  MM     M8  ,pm9MM  \n  A'     VML    MM  MM.   ,M9 8M   MM  \n.AMA.   .AMMA..JMML.P^YbmdP'  `Moo9^Yo.\n";

        internal const string ThumbnailBase = "https://files.coinmarketcap.com/static/img/coins/128x128/";

        internal const int UpdateSpan = 6000;

        internal const string UrlBase = "https://coinmarketcap.com/currencies/";

        internal static readonly Color InformationColor = new Color(0x0366d6);

        internal static readonly Color SuccessColor = new Color(0x28a745);

        internal static readonly Color FailureColor = new Color(0xcb2431);

        static readonly IEnumerable<string> KeywordKeys = new [] { "id", "name", "symbol" };

        internal static Dictionary<string, string> Config { get; set; }

        internal static DiscordSocketClient DiscordClient { get; set; }

        internal static CommandService Service { get; set; } = new CommandService();

        internal static CommandServiceConfig ServiceConfig { get; set; } = new CommandServiceConfig();

        internal static EmbedFooterBuilder CurrentFooter =>
            new EmbedFooterBuilder()
                .WithText("CoinMarketCap")
                .WithIconUrl("https://coinmarketcap.com/static/img/CoinMarketCap.png");

        internal static HttpClient CoinMarketCapClient { get; set; } = new HttpClient();

        internal static Dictionary<string, Dictionary<string, string>> Tickers { get; set; }

        static async Task Main(string[] args)
        {
            await SplashAsync();
            Config = await LoadConfigAsync(args.Length >= 1 ? args[0] : null);
            using (DiscordClient = new DiscordSocketClient())
            {
                await DiscordClient.LoginAsync(TokenType.Bot, Config["discord_token"]);
                await DiscordClient.StartAsync();
                DiscordClient.MessageReceived += message =>
                    Task.WhenAny(
                        HandleCommandAsync(message),
                        Task.Delay(0));
                DiscordClient.Ready += () =>
                    Task.WhenAny(
                        Service.AddModulesAsync(Assembly.GetEntryAssembly()),
                        OnReadyAsync(),
                        Task.Delay(0));
                await Task.Delay(-1);
            }
        }

        static async Task<Dictionary<string, Dictionary<string, string>>> GetTickersAsync()
        {
            using (var response = await CoinMarketCapClient.GetAsync(Endpoint))
            using (var content = response.Content)
            {
                var json = await content.ReadAsStringAsync();
                var deserialized = JsonConvert
                    .DeserializeObject<IEnumerable<Dictionary<string, string>>>(json);
                return KeywordKeys
                    .Select(key => deserialized
                        .Select(coin => new KeyValuePair<string, Dictionary<string, string>>(
                            coin.FirstOrDefault(x => x.Key == key).Value.ToLower(),
                            coin)))
                    .SelectMany(x => x)
                    .GroupBy(x => x.Key)
                    .Select(x => x.FirstOrDefault())
                    .ToDictionary(x => x.Key, x => x.Value);
            }
        }

        static async Task HandleCommandAsync(SocketMessage socketMessage)
        {
            var position = 0;
            var message = socketMessage as SocketUserMessage;
            var guildChannel = message.Channel as IGuildChannel;
            if (message == null ||
                !(
                    (message.HasMentionPrefix(DiscordClient.CurrentUser, ref position)) ||
                    message.HasStringPrefix(Prefix, ref position) ||
                    (
                        message.Channel is IDMChannel &&
                        message.Author.Id != DiscordClient.CurrentUser.Id
                    )
                )) return;
            var context = new CommandContext(DiscordClient, message);
            using (var typing = context.Channel.EnterTypingState())
            {
                var result = await Service.ExecuteAsync(context, position);
                if (!result.IsSuccess)
                {
                    await context.Channel.SendMessageAsync
                    (
                        text: context.User.Mention,
                        embed:
                            new EmbedBuilder()
                                .WithTitle("Command error")
                                .WithDescription(result.ErrorReason)
                                .WithCurrentTimestamp()
                                .WithColor(FailureColor)
                                .WithAuthor(context.User)
                    );
                }
            }
        }

        static async Task<Dictionary<string, string>> LoadConfigAsync(string path = null)
        {
            using (var stream = File.OpenText(string.IsNullOrEmpty(path) ? ConfigPath : path))
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(await stream.ReadToEndAsync());
            }
        }

        static async Task OnReadyAsync(CancellationToken token = default)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.WhenAll(
                    UpdateTickersAsync(),
                    Task.Delay(UpdateSpan)
                );
            }
        }

        static Task SplashAsync()
        {

            try
            {
                var assembly = Assembly.GetEntryAssembly();
                var version = ((AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute)))?.InformationalVersion ?? "";
                var copyright = ((AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCopyrightAttribute)))?.Copyright ?? "";
                return Console.Out.WriteLineAsync($"\n{SplashArt}\n           Version: {version}\n\n{copyright}\n");
            }
            catch
            {
                return Task.CompletedTask;
            }
        }

        static async Task UpdateTickersAsync()
        {
            try
            {
                Tickers = await GetTickersAsync();
            }
            catch { }
        }
    }
}
