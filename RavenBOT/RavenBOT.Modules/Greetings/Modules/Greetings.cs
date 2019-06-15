using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Modules.Greetings.Methods;

namespace RavenBOT.Modules.Greetings.Modules
{
    [Group("Greetings")]
    [RequireUserPermission(Discord.GuildPermission.Administrator)]
    [RequireContext(ContextType.Guild)]
    public class Greetings : InteractiveBase<ShardedCommandContext>
    {
        public GreetingsService GreetingsService { get; }
        public Greetings(GreetingsService greetingsService)
        {
            GreetingsService = greetingsService;
        }

        [Command("ToggleWelcome")]
        [Summary("Toggles the use of welcome messages")]
        public async Task ToggleWelcome()
        {
            var config = GreetingsService.GetWelcomeConfig(Context.Guild.Id);
            config.Enabled = !config.Enabled;
            GreetingsService.SaveWelcomeConfig(config);
            await ReplyAsync($"Display Welcome Messages: {config.Enabled}\nNOTE: You will need to run the `SetWelcomeChannel` command or enable dms in order for this to work.");
        }

        [Command("ToggleGoodbye")]
        [Summary("Toggles the use of goodbye messages")]
        public async Task ToggleGoodbye()
        {
            var config = GreetingsService.GetGoodbyeConfig(Context.Guild.Id);
            config.Enabled = !config.Enabled;
            GreetingsService.SaveGoodbyeConfig(config);
            await ReplyAsync($"Display Goodbye Messages: {config.Enabled}\nNOTE: You will need to run the `SetGoodbyeChannel` command or enable dms in order for this to work.");
        }

        [Command("SetWelcomeChannel")]
        [Summary("Sets the current channel for welcome messages")]
        public async Task SetWelcomeChannel()
        {
            var config = GreetingsService.GetWelcomeConfig(Context.Guild.Id);
            config.WelcomeChannel = Context.Channel.Id;
            GreetingsService.SaveWelcomeConfig(config);
            await ReplyAsync($"Display Welcome Messages: {config.Enabled}\n" +
                $"Welcome channel set to: {Context.Channel.Name}");
        }

        [Command("SetGoodbyeChannel")]
        [Summary("Sets the current channel for goodbye messages")]
        public async Task SetGoodbyeChannel()
        {
            var config = GreetingsService.GetGoodbyeConfig(Context.Guild.Id);
            config.GoodbyeChannel = Context.Channel.Id;
            GreetingsService.SaveGoodbyeConfig(config);
            await ReplyAsync($"Display Goodbye Messages: {config.Enabled}\n" +
                $"Goodbye channel set to: {Context.Channel.Name}");
        }

        [Command("ToggleWelcomeDms")]
        [Summary("Toggles the use of welcome messages in dms")]
        public async Task ToggleWelcomeDms()
        {
            var config = GreetingsService.GetWelcomeConfig(Context.Guild.Id);
            config.DirectMessage = !config.DirectMessage;
            GreetingsService.SaveWelcomeConfig(config);
            await ReplyAsync($"Display Welcome Messages: {config.Enabled}\n" +
                $"Direct Message Welcomes: {config.DirectMessage}");
        }

        [Command("ToggleGoodbyeDms")]
        [Summary("Toggles the use of goodbye messages in dms")]
        public async Task ToggleGoodbyeDms()
        {
            var config = GreetingsService.GetGoodbyeConfig(Context.Guild.Id);
            config.DirectMessage = !config.DirectMessage;
            GreetingsService.SaveGoodbyeConfig(config);
            await ReplyAsync($"Display Goodbye Messages: {config.Enabled}\n" +
                $"Direct Message Goodbyes: {config.DirectMessage}");
        }

        [Command("SetWelcomeMessage")]
        [Summary("Sets the welcome message")]
        public async Task SetWelcomeMessage([Remainder] string message = null)
        {
            var config = GreetingsService.GetWelcomeConfig(Context.Guild.Id);
            config.WelcomeMessage = message;
            GreetingsService.SaveWelcomeConfig(config);
            await ReplyAsync($"Display Welcome Messages: {config.Enabled}\n" +
                $"**Message**\n" +
                $"{message ?? "DEFAULT"}");
        }

        [Command("SetGoodbyeMessage")]
        [Summary("Sets the goodbye message")]
        public async Task SetGoodbyeMessage([Remainder] string message = null)
        {
            var config = GreetingsService.GetGoodbyeConfig(Context.Guild.Id);
            config.GoodbyeMessage = message;
            GreetingsService.SaveGoodbyeConfig(config);
            await ReplyAsync($"Display Goodbye Messages: {config.Enabled}\n" +
                $"**Message**\n" +
                $"{message ?? "DEFAULT"}");
        }

        [Command("MessageReplacements")]
        [Summary("Shows custom replacements to add more context to messages")]
        public async Task ShowMessageReplacements()
        {
            await ReplyAsync($"The following text snippets will be replaced in welcome and goodbye messages with their contextual counterparts\n" +
                "`{username}`\n" +
                "`{servername}`\n" +
                "`{userid}`\n" +
                "`{discriminator}`\n" +
                "`{mention}`\n" +
                "`{serverownerusername}`\n" +
                "`{serverownernickname}`\n" +
                "`{serverid}`\n");
        }
    }
}