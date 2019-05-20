using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Modules.AutoMod.Models.Moderation;

namespace RavenBOT.Modules.AutoMod.Modules
{
    public partial class Moderation : InteractiveBase<ShardedCommandContext>
    {
        //User Joins Server

        //User blocked from speaking etc.

        //User recieves code in dm

        //User runs command with code

        //If correct the user will get permissions to speak etc.

        //If incorrect try again, after 3 failures, kick

        [Command("Verify")]
        [RequireContext(ContextType.DM)]
        public async Task VerifyCaptcha(ulong guildId, [Remainder]string captcha = null)
        {
            if (captcha == null)
            {
                await ReplyAsync("You must provide a captcha to solve");
                return;
            }

            var captchaUser = ModerationService.GetCaptchaUser(Context.User.Id, guildId);
            if (captchaUser == null)
            {
                await ReplyAsync("Invalid guildId");
                return;
            }

            var config = ModerationService.GetModerationConfig(guildId);

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
                var role = guild.GetRole(config.CaptchaSettings.CaptchaTempRole);
                
                if (role != null && guildUser != null)
                {
                    await guildUser.RemoveRoleAsync(role);
                    ModerationService.SaveCaptchaUser(captchaUser);
                }

                await ReplyAsync("Successfully verified.");
                return;
            }

            if (captchaUser.FailureCount >= config.CaptchaSettings.MaxFailures)
            {
                await ModerationService.PerformCaptchaAction(config.CaptchaSettings.MaxFailuresAction, guildUser);
                await ReplyAsync("You have already exceeded the maximum attempt count.");
                return;
            }

            if (captcha.Equals(captchaUser.Captcha))
            {
                captchaUser.Passed = true;            

                var role = guild.GetRole(config.CaptchaSettings.CaptchaTempRole);
                
                if (role != null && guildUser != null)
                {
                    await guildUser.RemoveRoleAsync(role);
                    ModerationService.SaveCaptchaUser(captchaUser);
                    await ReplyAsync("Success, you have been verified.");
                    return;
                }

                await ReplyAsync("There was an error removing the role from your user. Please contact an admin.");
            }
            else
            {
                captchaUser.FailureCount++;

                await ReplyAsync($"You have failed attempt {captchaUser.FailureCount}/{config.CaptchaSettings.MaxFailures}");
                
                if (captchaUser.FailureCount >= config.CaptchaSettings.MaxFailures)
                {
                    await ReplyAsync("You have exceeded the maximum amount of attempts.");
                    await ModerationService.PerformCaptchaAction(config.CaptchaSettings.MaxFailuresAction, guildUser);
                }
                ModerationService.SaveCaptchaUser(captchaUser);
            }
        }

        [Command("UseCaptcha")]
        public async Task ToggleCaptcha()
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.UseCaptcha = !config.UseCaptcha;
            ModerationService.SaveModerationConfig(config);

            await ReplyAsync($"UseCaptcha: {config.UseCaptcha}");
        }

        [Command("MaxCaptchaWarnings")]
        public async Task SetCaptchaWarnings(int count = 3)
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            if (config.CaptchaSettings.SetMaxFailures(count))
            {
                ModerationService.SaveModerationConfig(config);
                await ReplyAsync("Max Failures for captcha: {count}");
            }
            else
            {
                await ReplyAsync($"Maximum failures must be greater than or equal to 1");
            }
        }

        [Command("CaptchaActions")]
        public async Task ShowCaptchaActions()
        {
            await ReplyAsync("Captcha Actions:\n" +
                            "`Ban`\n`Kick`\n`None`");
        }

        [Command("SetCaptchaAction")]
        public async Task SetCaptchaAction(ModerationConfig.Captcha.Action action = ModerationConfig.Captcha.Action.Kick)
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.CaptchaSettings.MaxFailuresAction = action;
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync($"Max Failure Action: {action}");
        }
    }
}