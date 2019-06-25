using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RavenBOT.Extensions;
using RavenBOT.Modules.Games.Models;

namespace RavenBOT.Modules.Games.Modules
{
    public partial class Game
    {
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

        private string none = ":black_circle:";
        private string blue = ":large_blue_circle:";
        private string red = ":red_circle:";

        [Command("Connect4", RunMode = RunMode.Async)]
        [Summary("Play connect 4 with another person")]
        public async Task Connect4Async(int bet = 0)
        {
            // I am wrapping this in a try catch until I finish finding all the bugs n shit.
            try
            {
                await Connect4InitializeTask(bet);
            }
            catch (Exception e)
            {
                await Connect4ErrorAsync(e);
            }
        }

        [Command("Connect4 Accept", RunMode = RunMode.Async)]
        [Summary("Accepts a connect 4 game")]
        public Task Connect4AcceptAsync()
        {
            return ReplyAsync("When accepting a connect4 game please do not use command prefixes. You only need to type `connect4 accept`");
        }

        public async Task Connect4ErrorAsync(Exception e)
        {
            // If there is an error, we need to ensure that the current channel can still initiate new games.
            await ReplyAsync(e.ToString().FixLength());
            var currentlobby = GameService.Connect4List.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            if (currentlobby != null)
            {
                currentlobby.GameRunning = false;
            }
        }

        public async Task Connect4InitializeTask(int bet = 0)
        {
            // Here we check whether or not there is currently a game running the the current lobby as there is no simple way to differentiate games at the moment (will add later)
            var currentlobby = GameService.Connect4List.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            if (currentlobby != null)
            {
                // We quit if there is a game already running
                if (currentlobby.GameRunning)
                {
                    await ReplyAsync(
                        "A game of connect4 is already running in this channel. Please wait until it is completed.");
                    return;
                }

                currentlobby.GameRunning = true;
            }
            else
            {
                // Add a lobby in the case that there hasn't been a game played in the cureent one yet.
                GameService.Connect4List.Add(new Connect4Game(Context.Channel.Id)
                {
                    GameRunning = true
                });
                currentlobby = GameService.Connect4List.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            }

            // Filter out invalid bets to make sure that games are played fairly
            if (bet <= 0)
            {
                // Ensure there is no negative or zero bet games.
                await ReplyAsync($"Please place a bet, ie. 10 Points!");
                currentlobby.GameRunning = false;
                return;
            }

            // Here get the the player 1's profile and ensure they are able to bet
            var initplayer1 = GameService.GetGameUser(Context.User.Id, Context.Guild.Id);
            if (bet > initplayer1.Points)
            {
                await ReplyAsync(
                    $"Your bet is too high, please place a bet less than or equal to {initplayer1.Points}");
                currentlobby.GameRunning = false;
                return;
            }

            await ReplyAsync("", false, new EmbedBuilder
            {
                Title = $"Connect4 Game (BET = {bet} Points)",
                    Description = "Get somebody to type `connect4 accept` to start"
            }.Build());

            await Connect4AcceptTask(bet, currentlobby);
        }

        public async Task Connect4AcceptTask(int bet, Connect4Game currentlobby)
        {
            var accepted = false;
            var timeoutattempts = 0;
            var messagewashout = 0;
            GameUser p2 = null;

            // Here we wait until another player accepts the game
            while (!accepted)
            {
                var next = await NextMessageAsync(false, true, TimeSpan.FromSeconds(10));
                if (next?.Author.Id == Context.User.Id)
                {
                    // Ignore author messages for the game until another player accepts
                }
                else if (string.Equals(next?.Content, "connect4 accept", StringComparison.InvariantCultureIgnoreCase))
                {
                    p2 = GameService.GetGameUser(next.Author.Id, Context.Guild.Id);
                    if (p2.Points < bet)
                    {
                        await ReplyAsync(
                            $"{next.Author.Mention} - You do not have enough points to play this game, you need a minimum of {bet}. Your balance is {p2.Points} points");
                    }
                    else
                    {
                        accepted = true;
                        p2 = GameService.GetGameUser(next.Author.Id, Context.Guild.Id);
                    }
                }

                // Overload for is a message is not sent within the timeout
                if (next == null)
                {
                    // if more than 6 timeouts (1 minute of no messages) occur without a user accepting, we quit the game
                    timeoutattempts++;
                    if (timeoutattempts < 6)
                    {
                        continue;
                    }

                    await ReplyAsync("Connect4: Timed out!");
                    currentlobby.GameRunning = false;
                    return;
                }

                // In case people are talking over the game, we also make the game quit after 25 messages are sent so it is not waiting indefinitely for a player to accept.
                messagewashout++;
                if (messagewashout <= 25)
                {
                    continue;
                }

                await ReplyAsync("Connect4: Timed out!");
                currentlobby.GameRunning = false;
                return;
            }

            await Connect4PreGameSetup(p2, bet, currentlobby);
        }

        public async Task Connect4PreGameSetup(GameUser p2, int bet, Connect4Game currentLobby)
        {
            var player1 = GameService.GetGameUser(Context.User.Id, Context.Guild.Id);
            var player2 = GameService.GetGameUser(p2.UserId, Context.Guild.Id);

            var lines = new int[6, 7];

            var embed = new EmbedBuilder();

            // Here we build the initial game board with all empty squares
            for (var r = 0; r < 6; r++)
            {
                if (r == 0)
                {
                    for (var c = 0; c < 7; c++)
                    {
                        embed.Description += $":{numlist[c]}:";
                    }

                    embed.Description += "\n";
                }

                for (var c = 0; c < 7; c++)
                {
                    embed.Description += $"{none}";
                }

                embed.Description += "\n";
            }

            embed.Description += "Usage:\n" +
                "`connect4 [column]`\n" +
                $":large_blue_circle: - {Context.User.Mention} <-\n" +
                $":red_circle: - {Context.Guild.GetUser(player2.UserId)?.Mention}";
            embed.Footer = new EmbedFooterBuilder
            {
                Text = $"it is {Context.User.Username}'s turn"
            };

            var gamemessage = await ReplyAsync("", false, embed.Build());

            await Connect4PlayingTask(lines, gamemessage, player1, player2, bet, embed, currentLobby);
        }

        public async Task Connect4PlayingTask(int[, ] lines, IUserMessage gamemessage, GameUser player1, GameUser player2, int bet, EmbedBuilder embed, Connect4Game currentlobby)
        {
            // LastX and LastY are used to check for horizontal and vertical wins
            var lastx = 0;
            var lasty = 0;
            var playinggame = true;

            // We always begin the game with whoever ran the initial command.
            var currentplayer = 1;
            var winmethod = "";

            // MSGTime is used to ensure that only a single minute passes in between turns, if it goes past a minute between turns then we count it as a player forfeiting.
            var msgtime = DateTime.UtcNow + TimeSpan.FromMinutes(1);
            var errormsgs = "";

            while (playinggame)
            {
                var errormsgs1 = errormsgs;
                if (!string.IsNullOrEmpty(errormsgs1))
                {
                    await gamemessage.ModifyAsync(x => x.Content = errormsgs1);
                }
                else
                {
                    await gamemessage.ModifyAsync(x => x.Content = " ");
                }

                errormsgs = "";

                // Using InteractiveBase we wait until a message is sent in the current game
                var next = await NextMessageAsync(false, true, TimeSpan.FromMinutes(1));

                // If the player doesn't show up mark them as forfeiting and award a win to the other player.
                if (next == null || msgtime < DateTime.UtcNow)
                {
                    await ReplyAsync(
                        $"{(currentplayer == 1 ? Context.Guild.GetUser(player1.UserId)?.Mention : Context.Guild.GetUser(player2.UserId)?.Mention)} Did not reply fast enough. Auto forfeiting");

                    var w = currentplayer == 1 ? player2.UserId : player1.UserId;
                    var l = currentplayer == 1 ? player1.UserId : player2.UserId;
                    await Connect4WinAsync(w, l, bet, "Player Forfeited.");
                    return;
                }

                // filter out non game messages by ignoring ones
                if (!next.Content.ToLower().StartsWith("connect4"))
                {
                    continue;
                }

                // Ensure that we only accept messages from players that are in the game
                if (next.Author.Id != player1.UserId && next.Author.Id != player2.UserId)
                {
                    // await ReplyAsync("You are not part of this game.");
                    errormsgs = $"{next.Author.Mention} You are not part of this game.";
                    await next.DeleteAsync();
                    continue;
                }

                // Ensure that the current message is from a player AND it is also their turn.
                if ((next.Author.Id == player1.UserId && currentplayer == 1) ||
                    (next.Author.Id == player2.UserId && currentplayer == 2))
                {
                    // filter out invalid line submissions
                    var parameters = next.Content.Split(" ");

                    // Make sure that the message is in the correct format of connect4 [line]
                    if (parameters.Length != 2 || !int.TryParse(parameters[1], out var Column))
                    {
                        errormsgs =
                            $"{(currentplayer == 2 ? Context.Guild.GetUser(player1.UserId)?.Mention : Context.Guild.GetUser(player2.UserId)?.Mention)} \n" +
                            $"Invalid Line input, here is an example input:\n" +
                            "`connect4 3` - this will place a counter in line 3.\n" +
                            "NOTE: Do not use the bot's prefix, just write `connect4 [line]`";
                        await next.DeleteAsync();
                        continue;
                    }

                    // as there are only 7 columns to pick from, filter out values outside of this range.
                    if (Column < 0 || Column > 6)
                    {
                        // error invalid line.
                        errormsgs =
                            $"{(currentplayer == 2 ? Context.Guild.GetUser(player1.UserId)?.Mention : Context.Guild.GetUser(player2.UserId)?.Mention)}\n" +
                            $"Invalid input, line number must be from 0-6 message in the format:\n" +
                            "`connect4 [line]`";
                        await next.DeleteAsync();
                        continue;
                    }

                    var success = false;

                    // moving from the top of the board downwards
                    for (var row = 5; row >= 0; row--)
                    {
                        if (lines[row, Column] != 0)
                        {
                            continue;
                        }

                        lines[row, Column] = currentplayer;
                        lastx = Column;
                        lasty = row;
                        success = true;
                        break;
                    }

                    // Ensure that we only move to the next player's turn IF the current player actually makes a move in an available column.
                    if (!success)
                    {
                        errormsgs =
                            $"{(currentplayer == 2 ? Context.Guild.GetUser(player1.UserId)?.Mention : Context.Guild.GetUser(player2.UserId)?.Mention)}\n" +
                            $"Error, please specify a line that isn't full";
                        await next.DeleteAsync();
                        continue;
                    }

                    // Update the embed message
                    embed.Description = "";
                    for (var r = 0; r < 6; r++)
                    {
                        if (r == 0)
                        {
                            for (var c = 0; c < 7; c++)
                            {
                                embed.Description += $":{numlist[c]}:";
                            }

                            embed.Description += "\n";
                        }

                        for (var c = 0; c < 7; c++)
                        {
                            if (lines[r, c] == 0)
                            {
                                embed.Description += $"{none}";
                            }
                            else if (lines[r, c] == 1)
                            {
                                embed.Description += $"{blue}";
                            }
                            else if (lines[r, c] == 2)
                            {
                                embed.Description += $"{red}";
                            }
                        }

                        embed.Description += "\n";
                    }

                    embed.Description += "Usage:\n" +
                        "`connect4 [column]`\n" +
                        $":large_blue_circle: - {Context.User.Mention} {(currentplayer == 1 ? "" : "<-")}\n" +
                        $":red_circle: - {Context.Guild.GetUser(player2.UserId)?.Mention} {(currentplayer == 2 ? "" : "<-")}";
                    embed.Footer = new EmbedFooterBuilder
                    {
                        Text =
                        $"it is {(currentplayer == 2 ? Context.Guild.GetUser(player1.UserId)?.Username : Context.Guild.GetUser(player2.UserId)?.Username)}'s turn"
                    };
                    await gamemessage.ModifyAsync(x => x.Embed = embed.Build());

                    // Check If it is a win here.
                    var connectioncount = 0;

                    // Checking Horizontally (Rows)
                    for (var i = 0; i <= 6; i++)
                    {
                        if (lines[lasty, i] == currentplayer)
                        {
                            connectioncount++;
                        }
                        else
                        {
                            connectioncount = 0;
                        }

                        if (connectioncount < 4)
                        {
                            continue;
                        }

                        // await ReplyAsync($"Player {currentplayer} Wins! Horizontal");
                        playinggame = false;
                        winmethod = "Horizontal";
                        break;
                    }

                    // Checking Vertically (Columns)
                    connectioncount = 0;
                    for (var i = 0; i <= 5; i++)
                    {
                        if (lines[i, lastx] == currentplayer)
                        {
                            connectioncount++;
                        }
                        else
                        {
                            connectioncount = 0;
                        }

                        if (connectioncount >= 4)
                        {
                            // await ReplyAsync($"Player {currentplayer} Wins! Vertical");
                            playinggame = false;
                            winmethod = "Vertical";
                            break;
                        }
                    }

                    /*     C    O    L    U    M    N    S                                      
                       R [0,0][0,1][0,2][0,3][0,4][0,5][0,6]
                       O [1,0][1,1][1,2][1,3][1,4][1,5][1,6]
                       W [2,0][2,1][2,2][2,3][2,4][2,5][2,6]
                       S [3,0][3,1][3,2][3,3][3,4][3,5][3,6]
                         [4,0][4,1][4,2][4,3][4,4][4,5][4,6]
                         [5,0][5,1][5,2][5,3][5,4][5,5][5,6]
                    */

                    // Checking Diagonally 
                    int colinit, rowinit;

                    // Top Left => Bottom Right (from top row diagonals)
                    for (rowinit = 0; rowinit <= 5; rowinit++)
                    {
                        connectioncount = 0;
                        int row, col;
                        for (row = rowinit, col = 0; col <= 6 && row <= 5; col++, row++)
                        {
                            if (lines[row, col] == currentplayer)
                            {
                                connectioncount++;
                                if (connectioncount < 4)
                                {
                                    continue;
                                }

                                playinggame = false;
                                winmethod = "Diagonal";
                                break;
                            }

                            connectioncount = 0;
                        }
                    }

                    // Top Left => Bottom Right (from columns)
                    for (colinit = 0; colinit <= 6; colinit++)
                    {
                        connectioncount = 0;
                        int row, col;
                        for (row = 0, col = colinit; col <= 6 && row <= 5; col++, row++)
                        {
                            if (lines[row, col] == currentplayer)
                            {
                                connectioncount++;
                                if (connectioncount < 4)
                                {
                                    continue;
                                }

                                playinggame = false;
                                winmethod = "Diagonal";
                                break;
                            }

                            connectioncount = 0;
                        }
                    }

                    // Checking other Diagonal.
                    // Top Right => Bottom Left
                    for (rowinit = 0; rowinit <= 5; rowinit++)
                    {
                        connectioncount = 0;
                        int row, col;
                        for (row = rowinit, col = 6; col >= 0 && row <= 5; col--, row++)
                        {
                            if (lines[row, col] == currentplayer)
                            {
                                connectioncount++;
                                if (connectioncount < 4)
                                {
                                    continue;
                                }

                                playinggame = false;
                                winmethod = "Diagonal";
                                break;
                            }

                            connectioncount = 0;
                        }
                    }

                    for (colinit = 6; colinit >= 0; colinit--)
                    {
                        connectioncount = 0;
                        int row, col;
                        for (row = 0, col = colinit; col >= 0 && row <= 5; col--, row++)
                        {
                            if (lines[row, col] == currentplayer)
                            {
                                connectioncount++;
                                if (connectioncount < 4)
                                {
                                    continue;
                                }

                                playinggame = false;
                                winmethod = "Diagonal";
                                break;
                            }

                            connectioncount = 0;
                        }
                    }

                    // If we have a win, do don't switch the current player.
                    if (!playinggame)
                    {
                        continue;
                    }

                    currentplayer = currentplayer == 1 ? 2 : 1;

                    // To reduce the amount of messages after the game, delete the connect4 message.
                    await next.DeleteAsync();
                    msgtime = DateTime.UtcNow + TimeSpan.FromMinutes(1);

                    if (!embed.Description.Contains($"{none}"))
                    {
                        // This means all spaces are filled on the board
                        // ie. a tie.
                        await ReplyAsync(
                            "The Game is a draw. User Balances have not been modified. Good Game!");
                        currentlobby.GameRunning = false;
                        return;
                    }
                }
                else
                {
                    errormsgs =
                        $"{(currentplayer == 2 ? Context.Guild.GetUser(player1.UserId)?.Mention : Context.Guild.GetUser(player2.UserId)?.Mention)}\n" +
                        "Unknown Player/Not your turn.";
                    await next.DeleteAsync();
                }
            }

            await Connect4GetResultsAsync(currentplayer, player1, player2, bet, winmethod);
        }

        public Task Connect4GetResultsAsync(int currentplayer, GameUser player1, GameUser player2, int bet, string winmethod)
        {
            // Finally now that the game is finished, go and modify user's scores.
            var winner = currentplayer == 1 ? player1 : player2;
            var loser = currentplayer == 1 ? player2 : player1;
            return Connect4WinAsync(winner.UserId, loser.UserId, bet, winmethod);
        }

        public Task Connect4WinAsync(ulong winnerID, ulong loserID, int bet, string winmethod)
        {
            // make sure we allow other connect4 games to be played in the current channel now.
            var currentlobby = GameService.Connect4List.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            currentlobby.GameRunning = false;

            // Get the users and modify their scores and stats.
            var gwinner = GameService.GetGameUser(winnerID, Context.Guild.Id);
            var gloser = GameService.GetGameUser(loserID, Context.Guild.Id);
            gwinner.Points = gwinner.Points - bet;
            gloser.Points = gloser.Points - bet;
            var payout = bet * 2;
            gwinner.Points = gwinner.Points + payout;
            gwinner.TotalBet = gwinner.TotalBet + bet;
            gloser.TotalBet = gloser.TotalBet + bet;
            gwinner.TotalWon = gwinner.TotalWon + payout;
            IUser winner = Context.Guild.GetUser(gwinner.UserId);
            IUser loser = Context.Guild.GetUser(gloser.UserId);

            GameService.SaveGameUser(gwinner);
            GameService.SaveGameUser(gloser);

            var embed2 = new EmbedBuilder
            {
                Title = $"{winner.Username} Wins!",
                Description = $"Winner: {winner.Username}\n" +
                $"Balance: {gwinner.Points} \n" +
                $"Payout: {payout}\n" +
                $"WinLine: {winmethod}\n" +
                $"Loser: {loser?.Username}\n" +
                $"Balance: {gloser.Points}\n" +
                $"Loss: {bet}"
            };
            return ReplyAsync("", false, embed2.Build());
        }
    }
}