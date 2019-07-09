using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace AcidChicken.Aiba
{
    using static Program;

    [Group("")]
    public class Module : ModuleBase
    {
        const string Separator = "~~―――――~~";

        [Command("price"), Summary("Returns price of currency which is specified.")]
        public Task PriceAsync([Summary("The currency name.")] params string[] keys) =>
            Task.WhenAll(keys.Select(async x =>
            {
                var ticker = Tickers?[x.ToLower()];
                var hourly = ticker["percent_change_1h"];
                var daily = ticker["percent_change_24h"];
                var weekly = ticker["percent_change_7d"];
                await Context.Channel.SendMessageAsync
                (
                    text: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle($"{ticker["name"]} ({ticker["symbol"]})")
                            .WithUrl($"{UrlBase}{ticker["id"]}")
                            .WithTimestamp(DateTimeOffset.FromUnixTimeSeconds(long.Parse(ticker["last_updated"])))
                            .WithColor(InformationColor)
                            .WithFooter(CurrentFooter)
                            .WithAuthor($"{(Context.User is IGuildUser user ? user.Nickname ?? user.Username : Context.User.Username)}#{Context.User.Discriminator}", Context.User.GetAvatarUrl())
                            .WithThumbnailUrl($"{ThumbnailBase}{ticker["id"]}.png")
                            .AddField(Separator, "*Prices*")
                            .AddField("BTC", $"{ticker["price_btc"]} BTC", true)
                            .AddField("USD", $"{ticker["price_usd"]} USD", true)
                            .AddField("JPY", $"{ticker["price_jpy"]} JPY", true)
                            .AddField(Separator, "*Changes*")
                            .AddField("Hourly", $"{(hourly.StartsWith("-") ? hourly : $"+{hourly}")} %", true)
                            .AddField("Daily", $"{(daily.StartsWith("-") ? daily : $"+{daily}")} %", true)
                            .AddField("Weekly", $"{(weekly.StartsWith("-") ? weekly : $"+{weekly}")} %", true)
                            .Build()
                );
            }));
    }
}
