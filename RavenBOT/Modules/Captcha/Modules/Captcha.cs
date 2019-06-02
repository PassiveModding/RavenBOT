using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Modules.Captcha.Methods;
using RavenBOT.Modules.Captcha.Models;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Captcha.Modules
{
    [Group("captcha.")]
    public class Captcha : InteractiveBase<ShardedCommandContext>
    {
        public CaptchaService CaptchaService {get;}
        public Captcha(CaptchaService captchaService)
        {
            CaptchaService = captchaService;
        }

        [Command("Verify")]
        public async Task VerifyCaptcha(ulong guildId, [Remainder]string captcha = null)
        {
            if (captcha == null)
            {
                await ReplyAsync("You must provide a captcha to solve");
                return;
            }

            var captchaUser = CaptchaService.GetCaptchaUser(Context.User.Id, guildId);
            if (captchaUser == null)
            {
                await ReplyAsync("Invalid guildId");
                return;
            }

            var config = CaptchaService.GetCaptchaConfig(guildId);

            var guild = Context.Client.GetGuild(guildId);
            if (guild == null)
            {
                await ReplyAsync("Invalid guild id provided");
                return;
            }
            var guildUser = guild.GetUser(Context.User.Id);
            if (guildUser == null)
            {
                await ReplyAsync("You aren't in that server.");
                return;
            }

            if (captchaUser.Passed)
            {
                var role = guild.GetRole(config.CaptchaTempRole);
                
                if (role != null && guildUser != null)
                {
                    await guildUser.RemoveRoleAsync(role);
                    CaptchaService.SaveCaptchaUser(captchaUser);
                }

                await ReplyAsync("Successfully verified.");
                return;
            }

            if (captchaUser.FailureCount >= config.MaxFailures)
            {
                await CaptchaService.PerformCaptchaAction(config.MaxFailuresAction, guildUser);
                await ReplyAsync("You have already exceeded the maximum attempt count.");
                return;
            }

            if (captcha.Equals(captchaUser.Captcha))
            {
                captchaUser.Passed = true;            

                var role = guild.GetRole(config.CaptchaTempRole);
                
                if (role != null && guildUser != null)
                {
                    await guildUser.RemoveRoleAsync(role);
                    CaptchaService.SaveCaptchaUser(captchaUser);
                    await ReplyAsync("Success, you have been verified.");
                    return;
                }

                await ReplyAsync("There was an error removing the role from your user. Please contact an admin.");
            }
            else
            {
                captchaUser.FailureCount++;

                await ReplyAsync($"You have failed attempt {captchaUser.FailureCount}/{config.MaxFailures}");
                
                if (captchaUser.FailureCount >= config.MaxFailures)
                {
                    await ReplyAsync("You have exceeded the maximum amount of attempts.");
                    await CaptchaService.PerformCaptchaAction(config.MaxFailuresAction, guildUser);
                }
                CaptchaService.SaveCaptchaUser(captchaUser);
            }
        }

        [Command("SetChannel")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task SetCaptchaChannel()
        {
            var config = CaptchaService.GetCaptchaConfig(Context.Guild.Id);
            config.ChannelId = Context.Channel.Id;
            CaptchaService.SaveCaptchaConfig(config);

            await ReplyAsync($"Captcha channel set to the current channel.");
        }

        [Command("UseCaptcha")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task ToggleCaptcha()
        {
            var config = CaptchaService.GetCaptchaConfig(Context.Guild.Id);
            config.UseCaptcha = !config.UseCaptcha;
            CaptchaService.SaveCaptchaConfig(config);

            await ReplyAsync($"UseCaptcha: {config.UseCaptcha}\n" + 
                            "Note: Please ensure you set a captcha channel (using the `SetChannel` command) as the bot will message there in the event that it cannot dm the user directly");
        }

        [Command("MaxCaptchaWarnings")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task SetCaptchaWarnings(int count = 3)
        {
            var config = CaptchaService.GetCaptchaConfig(Context.Guild.Id);
            if (config.SetMaxFailures(count))
            {
                CaptchaService.SaveCaptchaConfig(config);
                await ReplyAsync("Max Failures for captcha: {count}");
            }
            else
            {
                await ReplyAsync($"Maximum failures must be greater than or equal to 1");
            }
        }

        [Command("CaptchaActions")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task ShowCaptchaActions()
        {
            await ReplyAsync("Captcha Actions:\n" +
                            "`Ban`\n`Kick`\n`None`");
        }

        [Command("SetCaptchaAction")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task SetCaptchaAction(CaptchaConfig.Action action = CaptchaConfig.Action.Kick)
        {
            var config = CaptchaService.GetCaptchaConfig(Context.Guild.Id);
            config.MaxFailuresAction = action;
            CaptchaService.SaveCaptchaConfig(config);
            await ReplyAsync($"Max Failure Action: {action}");
        }

        [Command("CaptchaSettings")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task CaptchaSettings()
        {
            var config = CaptchaService.GetCaptchaConfig(Context.Guild.Id);
            await ReplyAsync($"**CAPTCHA SETTINGS**\n" +
                            $"Use Captcha: {config.UseCaptcha}\n" +
                            $"Temp Role: {Context.Guild.GetRole(config.CaptchaTempRole)?.Mention ?? "N/A"}\n" +
                            $"Max Captcha Failures: {config.MaxFailures}\n" +
                            $"Max Captcha Failures Action: {config.MaxFailuresAction}\n" + 
                            $"Captcha hannel: {Context.Guild.GetTextChannel(config.ChannelId)?.Mention ?? "N/A, it is recommended that you set this asap"}");
        }
    }
}