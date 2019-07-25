using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common;
using RavenBOT.Modules.Weather.Methods;
using RavenBOT.Modules.Weather.Models;

namespace RavenBOT.Modules.Weather.Modules
{
    [Group("Weather")]
    public class Weather : InteractiveBase<ShardedCommandContext>
    {
        public Weather(WeatherService service)
        {
            Service = service;
        }

        public WeatherService Service { get; }

        [Command]
        [Summary("Searches for the specified location and returns current and weekly weather forecase")]
        public async Task SearchAsync([Remainder] string search)
        {
            var geoCodeResponse = await Service.GeocodeAsync(search);
            if (geoCodeResponse == null)
            {
                await ReplyAsync("Error making weather request.");
                return;
            }
            else if (geoCodeResponse.Count == 0)
            {
                await ReplyAsync("Unknown Location, please check for spelling mistakes or be more specific.");
                return;
            }
            else if (geoCodeResponse.Count > 1)
            {
                await ReplyAsync($"Multiple locations were found, taking the first result \"{geoCodeResponse.First["display_name"]}\"");

                /*var response = $"Location Name too common, {geoCodeResponse.Count} Results (MAX=5) please be more specific in the location name."+
                $"\nTry specifying some of the following: State/Province/Territory, Country, Post Code, Street, Address."+
                $"\nResults:\n{string.Join("\n", geoCodeResponse.Take(5).Select(x => $"\"{x["display_name"]}\""))}";
                await ReplyAsync(response); */

                //return;            
            }

            var weatherResponse = await Service.GetWeatherAsync(geoCodeResponse.First);
            if (weatherResponse.Item2 == null)
            {
                await ReplyAsync("Weather API Not Available, this may be due to either hitting the api limit of the bot owner not setting the api key.");
                return;
            }
            var response = weatherResponse.Item2;
            var embed = new EmbedBuilder
            {
                Color = Discord.Color.Green,
                Title = weatherResponse.Item1
            };
            embed.Title = weatherResponse.Item1;

            TimeSpan offset = TimeSpan.FromHours(response.Offset);
            var currentTime = Service.UnixTime(response.Currently.Time, offset);

            var weekSummary = response.Daily.Data.Select(x =>
            {
                var time = Service.UnixTime(x.Time, offset);
                return $"**{time.DayOfWeek}** \n{x.ApparentTemperatureMax}°F / {x.ApparentTemperatureMin}°F ( {Service.FtoC(x.ApparentTemperatureMax)}°C / {Service.FtoC(x.ApparentTemperatureMin)}°C ) \n{x.Summary}";
            }).ToArray();

            embed.Description = 
                    $"**Feels Like:** {response.Currently.ApparentTemperature}°F / {Service.FtoC(response.Currently.ApparentTemperature)}°C\n" +
                    $"**Actual:** {response.Currently.Temperature}°F / {Service.FtoC(response.Currently.Temperature)}°C\n";
            embed.AddField(new Discord.EmbedFieldBuilder
            {
                Name = "Week",
                Value = string.Join("\n", weekSummary)
            });
            embed.Author = new EmbedAuthorBuilder
            {
                Name = $"Currently: {response.Currently.Summary}",
                IconUrl = $"https://darksky.net/images/weather-icons/{response.Currently.Icon}.png"
            };
            embed.Footer = new EmbedFooterBuilder()
            {
                Text = $"Local Time: {currentTime.DayOfWeek} {currentTime.DateTime.ToShortDateString()} {currentTime.DateTime.ToShortTimeString()} || Powered by DarkSky https://darksky.net/poweredby/"
            };

            await ReplyAsync("", false, embed.Build());
        }

        [Priority(100)]
        [Command("SetApiKey")]
        [RavenRequireOwner]
        public async Task SetApiKey(string key = null)
        {
            var config = Service.Database.Load<DarkSkyConfig>(DarkSkyConfig.DocumentName());
            if (config == null)
            {
                config = new DarkSkyConfig();
            }
            config.ApiKey = key;
            Service.ApiKey = key;
            Service.Database.Store(config, DarkSkyConfig.DocumentName());
            await ReplyAsync("", false, $"Api Key set to: {key ?? "null"}".QuickEmbed());
        }
    }
}