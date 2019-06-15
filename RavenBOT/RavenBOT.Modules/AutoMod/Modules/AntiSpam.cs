using System.Threading.Tasks;
using Discord.Commands;

namespace RavenBOT.Modules.AutoMod.Modules
{
    public partial class Moderation
    {
        [Command("UseAntiSpam")]
        [Summary("Toggles the use of anti-spam methods")]
        public async Task ToggleAntiSpam()
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.UseAntiSpam = !config.UseAntiSpam;
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync($"Use AntiSpam: {config.UseAntiSpam}\n" +
                            $"Cache Size: {config.SpamSettings.CacheSize}\n" +
                            $"Max Message Repititions per {config.SpamSettings.CacheSize} Messages: {config.SpamSettings.MaxRepititions}\n" + 
                            $"Seconds to Check for Spam: {config.SpamSettings.SecondsToCheck}\n" +
                            $"Messages per {config.SpamSettings.SecondsToCheck} Seconds: {config.SpamSettings.MessagesPerTime}");
        }

        [Command("SetAntiSpamCacheSize")]
        [Summary("Sets the amount of messages to be checked against when using max repetitions")]
        public async Task SetCacheSize(int size)
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            var result = config.SpamSettings.SetCacheSize(size);
            if (result)
            {
                ModerationService.SaveModerationConfig(config);
                await ReplyAsync("Cache Size Set.");
            }
            else
            {
                await ReplyAsync($"Cache size must be greater than max repititions or messages per x seconds:\n" +
                            $"Current Cache Size: {config.SpamSettings.CacheSize}\n" +
                            $"Max Message Repititions per {config.SpamSettings.CacheSize} Messages: {config.SpamSettings.MaxRepititions}\n" + 
                            $"Messages per {config.SpamSettings.SecondsToCheck} Seconds: {config.SpamSettings.MessagesPerTime}");
            }
        }

        [Command("SetMaxRepititions")]
        [Summary("Sets the maximum amount of identical messages before a user is marked for spam")]
        public async Task SetMaxRepititions(int count)
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            var result = config.SpamSettings.SetMaxRepititions(count);
            if (result)
            {
                ModerationService.SaveModerationConfig(config);
                await ReplyAsync("Max Repititions Set.");
            }
            else
            {
                await ReplyAsync($"Repitition count must be less than the cache size:\n" +
                            $"Current Cache Size: {config.SpamSettings.CacheSize}");
            }
        }

        [Command("SetMessagesPerTime")]
        [Summary("Sets the amount of messages a user can say in a limited amount of time")]
        public async Task SetMessagesPerTime(int count)
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            var result = config.SpamSettings.SetMaxMessagesPerTime(count);
            if (result)
            {
                ModerationService.SaveModerationConfig(config);
                await ReplyAsync($"Max Messages per {config.SpamSettings.SecondsToCheck} Seconds Set.");
            }
            else
            {
                await ReplyAsync($"Message count must be less than the cache size:\n" +
                            $"Current Cache Size: {config.SpamSettings.CacheSize}");
            }
        }

        [Command("SetTimeLimit")]
        [Summary("Sets the amount of time the amount of messages set by `SetMessagesPerTime` will check against")]
        public async Task SetTimeLimit(int seconds)
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.SpamSettings.SecondsToCheck = seconds;
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync("Time Limit Set.");
        }
    }
}