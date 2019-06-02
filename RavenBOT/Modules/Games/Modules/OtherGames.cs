using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;

namespace RavenBOT.Modules.Games.Modules
{
    [Group("Games")]
    public class OtherGames : InteractiveBase<ShardedCommandContext>
    {
        public Random Random {get;}
        public OtherGames(Random random)
        {
            Random = random;
        }

        [Command("8ball")]
        [Summary("8ball <question?>")]
        [Remarks("ask me anything")]
        public async Task Ball([Remainder]string input = null)
        {
            if (input == null)
            {
                await ReplyAsync($"Ask me a question silly, eg. `8ball am I special?`");
            }
            else
            {
                var embed = new EmbedBuilder
                {
                    Description = $"❓ {input}\n 🎱 {Answers8Ball[Random.Next(0, Answers8Ball.Length)]}"
                };

                await ReplyAsync("", false, embed.Build());
            }
        }

        [Command("Fortune")]
        [Summary("fortune")]
        [Remarks("open a fortune cookie")]
        public async Task FortuneAsync()
        {
            var embed = new EmbedBuilder
            {
                Description = $"{Fortune[Random.Next(0, Fortune.Length)]}"
            };
            await ReplyAsync("", false, embed.Build());
        }

        [Command("rps")]
        [Summary("rps <r, p or s>")]
        [Remarks("rock paper scissors!")]
        public async Task Rps(string input = null)
        {
            if (input == null)
            {
                await ReplyAsync(
                    "❓ to play rock, paper, scissors" +
                    $"\n\n:waning_gibbous_moon: type `rps rock` or `rps r` to pick rock" +
                    $"\n\n:newspaper: type `rps paper` or `rps p` to pick paper" +
                    $"\n\n✂️ type `rps scissors` or `rps s` to pick scissors"
                );
            }
            else
            {
                int pick;
                switch (input)
                {
                    case "r":
                    case "rock":
                        pick = 0;
                        break;
                    case "p":
                    case "paper":
                        pick = 1;
                        break;
                    case "scissors":
                    case "s":
                        pick = 2;
                        break;
                    default:
                        return;
                }

                var choice = Random.Next(0, 3);

                string msg;
                if (pick == choice)
                    msg = "We both chose: " + GetRpsPick(pick) + " Draw, Try again";
                else if (pick == 0 && choice == 1 ||
                         pick == 1 && choice == 2 ||
                         pick == 2 && choice == 0)
                    msg = "My Pick: " + GetRpsPick(choice) + "Beats Your Pick: " + GetRpsPick(pick) +
                          "\nYou Lose! Try Again!";
                else
                    msg = "Your Pick: " + GetRpsPick(pick) + "Beats My Pick: " + GetRpsPick(choice) +
                          "\nCongratulations! You win!";


                var embed = new EmbedBuilder
                {
                    Description = $"{msg}"
                };
                await ReplyAsync("", false, embed.Build());
            }
        }

        private static string GetRpsPick(int p)
        {
            switch (p)
            {
                case 0:
                    return ":waning_gibbous_moon: ";
                case 1:
                    return ":newspaper:";
                default:
                    return "✂️";
            }
        }

        [Command("coin")]
        [Summary("coin")]
        [Remarks("Flips a coin")]
        public async Task Coin()
        {
            var val = Random.Next(0, 100);
            string result;
            string coin;
            if (val >= 50)
            {
                coin =
                    "https://www.random.org/coins/faces/60-usd/0010c/reverse.jpg";
                result = "You Flipped **Tails!!**";
            }
            else
            {
                coin =
                    "https://www.random.org/coins/faces/60-usd/0010c/obverse.jpg";
                result = "You Flipped **Heads!!**";
            }

            var embed = new EmbedBuilder
            {
                ImageUrl = coin,
                Description = result
            };
            await ReplyAsync("", false, embed.Build());
        }

        [Command("dice")]
        [Summary("dice")]
        [Alias("roll")]
        [Remarks("roll a dice")]
        public async Task RollDice()
        {
            var embed = new EmbedBuilder
            {
                Title = ":game_die: I Rolled A Die :game_die:",
                ImageUrl = $"{Dice[Random.Next(0, Dice.Length)]}"
            };

            await ReplyAsync("", false, embed.Build());
        }

        public string[] Dice =
        {
            "https://www.wpclipart.com/recreation/games/dice/die_face_1.png",
            "https://www.wpclipart.com/recreation/games/dice/die_face_2.png",
            "https://www.wpclipart.com/recreation/games/dice/die_face_3.png",
            "https://www.wpclipart.com/recreation/games/dice/die_face_4.png",
            "https://www.wpclipart.com/recreation/games/dice/die_face_5.png",
            "https://www.wpclipart.com/recreation/games/dice/die_face_6.png"
        };

        public string[] Fortune =
        {
            "Today it's up to you to create the peacefulness you long for.",
            "A friend asks only for your time not your money.",
            "If you refuse to accept anything but the best, you very often get it.",
            "A smile is your passport into the hearts of others.",
            "A good way to keep healthy is to eat more Chinese food.",
            "Your high-minded principles spell success.",
            "Hard work pays off in the future, laziness pays off now.",
            "Change can hurt, but it leads a path to something better.",
            "Enjoy the good luck a companion brings you.",
            "People are naturally attracted to you.",
            "Hidden in a valley beside an open stream- This will be the type of place where you will find your dream.",
            "A chance meeting opens new doors to success and friendship.",
            "You learn from your mistakes... You will learn a lot today.",
            "If you have something good in your life, don't let it go!",
            "What ever you're goal is in life, embrace it visualize it, and for it will be yours.",
            "Your shoes will make you happy today.",
            "You cannot love life until you live the life you love.",
            "Be on the lookout for coming events; They cast their shadows beforehand.",
            "Land is always on the mind of a flying bird.",
            "The man or woman you desire feels the same about you.",
            "Meeting adversity well is the source of your strength.",
            "A dream you have will come true.",
            "Our deeds determine us, as much as we determine our deeds.",
            "Never give up. You're not a failure if you don't give up.",
            "You will become great if you believe in yourself.",
            "There is no greater pleasure than seeing your loved ones prosper.",
            "You will marry your lover.",
            "A very attractive person has a message for you.",
            "You already know the answer to the questions lingering inside your head.",
            "It is now, and in this world, that we must live.",
            "You must try, or hate yourself for not trying.",
            "You can make your own happiness.",
            "The greatest risk is not taking one.",
            "The love of your life is stepping into your planet this summer.",
            "Love can last a lifetime, if you want it to.",
            "Adversity is the parent of virtue.",
            "Serious trouble will bypass you.",
            "A short stranger will soon enter your life with blessings to share.",
            "Now is the time to try something new.",
            "Wealth awaits you very soon.",
            "If you feel you are right, stand firmly by your convictions.",
            "If winter comes, can spring be far behind?",
            "Keep your eye out for someone special.",
            "You are very talented in many ways.",
            "A stranger, is a friend you have not spoken to yet.",
            "A new voyage will fill your life with untold memories.",
            "You will travel to many exotic places in your lifetime.",
            "Your ability for accomplishment will follow with success.",
            "Nothing astonishes men so much as common sense and plain dealing.",
            "Its amazing how much good you can do if you dont care who gets the credit.",
            "Everyone agrees. You are the best.",
            "LIFE CONSIST NOT IN HOLDING GOOD CARDS, BUT IN PLAYING THOSE YOU HOLD WELL.",
            "Jealousy doesn't open doors, it closes them!",
            "It's better to be alone sometimes.",
            "When fear hurts you, conquer it and defeat it!",
            "Let the deeds speak.",
            "You will be called in to fulfill a position of high honor and responsibility.",
            "The man on the top of the mountain did not fall there.",
            "You will conquer obstacles to achieve success.",
            "Joys are often the shadows, cast by sorrows.",
            "Fortune favors the brave.",
            "An upward movement initiated in time can counteract fate.",
            "A journey of a thousand miles begins with a single step.",
            "Sometimes you just need to lay on the floor.",
            "Never give up. Always find a reason to keep trying.",
            "If you have something worth fighting for, then fight for it.",
            "Stop wishing. Start doing.",
            "Accept your past without regrets. Handle your present with confidence. Face your future without fear.",
            "Stay true to those who would do the same for you.",
            "Ask yourself if what you are doing today is getting you closer to where you want to be tomorrow.",
            "Happiness is an activity.",
            "Help is always needed but not always appreciated. Stay true to your heart and help those in need weather they appreciate it or not.",
            "Hone your competitive instincts.",
            "Finish your work on hand don't be greedy.",
            "For success today, look first to yourself.",
            "Your fortune is as sweet as a cookie.",
            "Integrity is the essence of everything successful.",
            "If you're happy, you're successful.",
            "You will always be surrounded by true friends",
            "Believing that you are beautiful will make you appear beautiful to others around you.",
            "Happinees comes from a good life.",
            "Before trying to please others think of what makes you happy.",
            "When hungry, order more Chinese food.",
            "Your golden opportunity is coming shortly.",
            "For hate is never conquered by hate. Hate is conquered by love .",
            "You will make many changes before settling down happily.",
            "A man is born to live and not prepare to live.",
            "You cannot become rich except by enriching others.",
            "Don't pursue happiness - create it.",
            "You will be successful in love.",
            "All your fingers can't be of the same length.",
            "Wise sayings often fall on barren ground, but a kind word is never thrown away.",
            "A lifetime of happiness is in store for you.",
            "It is very possible that you will achieve greatness in your lifetime.",
            "Be tactful; overlook your own opportunity.",
            "You are the controller of your destiny.",
            "Everything happens for a reson.",
            "How can you have a beutiful ending without making beautiful mistakes.",
            "You can open doors with your charm and patience.",
            "Welcome the change coming into your life."
        };

        public string[] Answers8Ball =
        {
            "It is certain",
            "It is decidedly so",
            "Without a doubt",
            "Yes definitely",
            "You may rely on it",
            "As I see it, yes",
            "Most likely",
            "Outlook good",
            "Yes",
            "Signs point to yes",
            "Reply hazy try again",
            "Ask again later",
            "Better not tell you now",
            "Cannot predict now",
            "Concentrate and ask again",
            "Don't count on it",
            "My reply is no",
            "My sources say no",
            "Outlook not so good",
            "Very doubtful"
        };
    }
}