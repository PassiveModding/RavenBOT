using System;
using System.Collections.Generic;    
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;
using RavenBOT.Extensions;

namespace RavenBOT.Modules.Games.Modules
{
    [Group("Games")]
    public class Trivia : InteractiveBase<ShardedCommandContext>
    {
        public Random Random {get;}
        public HttpClient HttpClient {get;}

        public Trivia(Random random, HttpClient client)
        {
            Random = random;
            HttpClient = client;
        }

        private readonly List<string> numlist = new List<string>
                                                {
                                                    "zero",
                                                    "one",
                                                    "two",
                                                    "three",
                                                    "four",
                                                    "five",
                                                    "six",
                                                    "seven",
                                                    "eight",
                                                    "nine"
                                                };

        [Command("TriviaSettings", RunMode = RunMode.Async)]
        [Summary("Displays possible trivia settings and categories")]
        public async Task TriviaSettingsAsync()
        {
            if (categories == null)
            {
                await PopulateCategories();
            }

            await ReplyAsync($"**Trivia Command:**\n" +
                                   $"`Trivia <Question_Count> <Category_Name> <Difficulty> <Type>`\n" +
                                   $"**Example:**\n" +
                                   $"`Trivia 10 \"General Knowledge\" Medium Multiple`\n" +
                                   $"**Categories:**\n" +
                                   $"{(categories == null ? "N/A" : string.Join("\n", categories.trivia_categories.Select(x => $"\"{x.name}\"")))}\n" +
                                   $"**Difficulties:**\n" +
                                   $"{string.Join("\n", Enum.GetNames(typeof(TriviaDifficulty)))}\n" +
                                   $"**Types:**\n" +
                                   $"{string.Join("\n", Enum.GetNames(typeof(TriviaType)))}");
        }

        [Command("Trivia", RunMode = RunMode.Async)]
        [Summary("Play a game of trivia")]
        [RequireContext(ContextType.Guild)]
        public async Task TriviaAsync(int questions = 10, string categoryName = null, TriviaDifficulty difficulty = TriviaDifficulty.none, TriviaType type = TriviaType.none)
        {
            TriviaCategories.TriviaCategory category = null;
            if (categoryName != null)
            {
                if (categories == null)
                {
                    await PopulateCategories();
                }

                category = categories?.trivia_categories.FirstOrDefault(x => x.name.Equals(categoryName, StringComparison.InvariantCultureIgnoreCase));
            }

            var triviaData = await NewTriviaAsync(Context.Guild.Id, questions, difficulty, type, category);
            if (triviaData != null)
            {
                await NextQuestionAsync(triviaData, 0, 0);
                return;
            }

            await ReplyAsync("Unable to get trivia questions");
        }

        public async Task NextQuestionAsync(TriviaResponse trivia, int currentQuestionIndex, int totalCorrect, string resString = "")
        {
            TriviaResponse.Result question = null;
            try
            {
                question = trivia.results[currentQuestionIndex];
            }
            catch
            {
                // Ignore out of bounds error
            }

            if (question != null)
            {
                var possibleAnswers = question.incorrect_answers;
                possibleAnswers.Add(question.correct_answer);
                possibleAnswers = possibleAnswers.OrderByDescending(x => Random.Next()).ToList();

                var builder = new EmbedBuilder();
                builder.Color = Color.DarkPurple;
                var possibleAnswersString = "";
                int index = 0;
                foreach (var answer in possibleAnswers)
                {
                    possibleAnswersString += $":{numlist[index]}: {answer}\n";
                    index++;
                }

                builder.Description = $"Category: **{question.category}**\n" +
                                           $"Difficulty: **{question.difficulty}**\n" +
                                           $"**{question.question}**\n" +
                                           $"{possibleAnswersString}";

                builder.Footer = new EmbedFooterBuilder
                                     {
                                         Text = $"Progress: {currentQuestionIndex}/{trivia.results.Count} | Correct: {totalCorrect}/{currentQuestionIndex}"
                                     };

                var callBackData = new ReactionCallbackData("", builder.Build(), true, true, TimeSpan.FromMinutes(5));
                index = 0;
                foreach (var answer in possibleAnswers)
                {
                    var correct = answer.Equals(question.correct_answer, StringComparison.InvariantCultureIgnoreCase);
                    var newTotalCorrect = correct ? totalCorrect + 1 : totalCorrect;
                    var newResString = resString + $"{Format.Bold(question.question)} \n{(correct ? $":white_check_mark: {question.correct_answer}" : $":x: {Format.Strikethrough(answer)} {question.correct_answer}")}\n";

                    callBackData.WithCallback(emotes[index], async (c, r) =>
                        {
                            var rMessage = await c?.Channel?.GetMessageAsync(r?.MessageId ?? 0);
                            if (rMessage != null)
                            {
                                await rMessage.DeleteAsync();
                            }

                            await NextQuestionAsync(trivia, currentQuestionIndex + 1, newTotalCorrect, newResString);
                        });
                    index++;
                }

                await InlineReactionReplyAsync(callBackData);
                return;
            }

            await ReplyAsync($"Trivia Complete! You got {totalCorrect}/{trivia.results.Count} correct\n" + 
                                   $"{resString}".FixLength(2000));
        }

        private List<Emoji> emotes = new List<Emoji>
                                                    {
                                                        new Emoji("0\u20e3"),
                                                        new Emoji("1\u20e3"),
                                                        new Emoji("2\u20e3"),
                                                        new Emoji("3\u20e3"),
                                                        new Emoji("4\u20e3"),
                                                        new Emoji("5\u20e3"),
                                                        new Emoji("6\u20e3"),
                                                        new Emoji("7\u20e3"),
                                                        new Emoji("8\u20e3"),
                                                        new Emoji("9\u20e3")
                                                    };

        private async Task PopulateCategories()
        {
            try
            {
                var url = "https://opentdb.com/api_category.php";
                var res = await HttpClient.GetStringAsync(url);

                var resObject = JsonConvert.DeserializeObject<TriviaCategories>(res);
                categories = resObject;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private readonly Dictionary<ulong, SessionTokenParent> sessionTokens = new Dictionary<ulong, SessionTokenParent>();

        private TriviaCategories categories;

        public class SessionTokenParent
        {
            public SessionTokenParent(ulong guildId, SessionToken token)
            {
                this.guildId = guildId;
                tokenObject = token;
                lastUse = DateTime.UtcNow;
            }

            public ulong guildId { get; set; }

            public DateTime lastUse { get; set; }

            public SessionToken tokenObject { get; set; }

            public class SessionToken
            {
                public int response_code { get; set; }

                public string response_message { get; set; }

                public string token { get; set; }
            }
        }

        public enum TriviaType
        {
            boolean,
            multiple,
            none
        }

        public enum TriviaDifficulty
        {
            easy,
            medium,
            hard,
            none
        }

        private async Task<string> GetSessionToken(ulong guildId)
        {
            try
            {
                if (sessionTokens.TryGetValue(guildId, out var tokenParent))
                {
                    if (tokenParent.lastUse + TimeSpan.FromHours(5) > DateTime.UtcNow)
                    {
                        var newToken = await GenerateSessionTokenAsync();
                        if (newToken != null)
                        {
                            tokenParent.tokenObject = newToken;
                            tokenParent.lastUse = DateTime.UtcNow;
                            return tokenParent.tokenObject.token;
                        }

                        sessionTokens.Remove(guildId);
                        return null;
                    }

                    tokenParent.lastUse = DateTime.UtcNow;
                    return tokenParent.tokenObject.token;
                }

                var sessionToken = await GenerateSessionTokenAsync();
                if (sessionToken != null)
                {
                    var newParent = new SessionTokenParent(guildId, sessionToken);
                    sessionTokens.Add(guildId, newParent);

                    return newParent.tokenObject.token;
                }

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        private async Task<SessionTokenParent.SessionToken> GenerateSessionTokenAsync()
        {
            try
            {
                var baseUrl = "https://opentdb.com/api_token.php?command=request";
                var res = await HttpClient.GetStringAsync(baseUrl);

                var resObject = JsonConvert.DeserializeObject<SessionTokenParent.SessionToken>(res);
                if (resObject.response_code == 0)
                {
                    return resObject;
                }

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        private async Task<TriviaResponse> NewTriviaAsync(ulong guildId, int questions, TriviaDifficulty difficulty, TriviaType type, TriviaCategories.TriviaCategory category = null)
        {
            try
            {
                if (questions <= 0)
                {
                    return null;
                }

                var token = await GetSessionToken(guildId);
                var baseUrl = $"https://opentdb.com/api.php?amount={questions}&encode=base64";

                if (token != null)
                {
                    baseUrl += $"&token={token}";
                }

                if (difficulty != TriviaDifficulty.none)
                {
                    baseUrl += $"&difficulty={difficulty.ToString()}";
                }

                if (type != TriviaType.none)
                {
                    baseUrl += $"&type={type.ToString()}";
                }

                if (category != null)
                {
                    baseUrl += $"&category={category.id}";
                }

                var res = await HttpClient.GetStringAsync(baseUrl);
                var resObject = JsonConvert.DeserializeObject<TriviaResponse>(res);

                if (resObject.response_code == 0)
                {
                    resObject.results = resObject.results.Select(x => new TriviaResponse.Result
                                                                          {
                                                                              category = x.category.DecodeBase64(),
                                                                              correct_answer = x.correct_answer.DecodeBase64(),
                                                                              difficulty = x.difficulty.DecodeBase64(),
                                                                              question = x.question.DecodeBase64(),
                                                                              type = x.type.DecodeBase64(),
                                                                              incorrect_answers = x.incorrect_answers.Select(ia => ia.DecodeBase64()).ToList()
                                                                          }).ToList();

                    return resObject;
                }

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public class TriviaCategories
        {
            public List<TriviaCategory> trivia_categories { get; set; }

            public class TriviaCategory
            {
                public int id { get; set; }
                public string name { get; set; }
            }
        }

        public class TriviaResponse
        {
            public int response_code { get; set; }

            public List<Result> results { get; set; }

            public class Result
            {
                public string category { get; set; }

                public string type { get; set; }

                public string difficulty { get; set; }

                public string question { get; set; }

                public string correct_answer { get; set; }

                public List<string> incorrect_answers { get; set; }
            }
        }
    }
}