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
                            .WithAuthor(Context.User)
                            .WithThumbnailUrl($"{ThumbnailBase}{ticker["id"]}.png")
                            .AddField(Separator, "*Prices*")
                            .AddInlineField("BTC", $"{ticker["price_btc"]} BTC")
                            .AddInlineField("USD", $"{ticker["price_usd"]} USD")
                            .AddInlineField("JPY", $"{ticker["price_jpy"]} JPY")
                            .AddField(Separator, "*Changes*")
                            .AddInlineField("Hourly", $"{(hourly.StartsWith("-") ? hourly : $"+{hourly}")} %")
                            .AddInlineField("Daily", $"{(daily.StartsWith("-") ? daily : $"+{daily}")} %")
                            .AddInlineField("Weekly", $"{(weekly.StartsWith("-") ? weekly : $"+{weekly}")} %")
                );
            }));
    }
}
