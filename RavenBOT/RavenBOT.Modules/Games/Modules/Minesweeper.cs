using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;

namespace RavenBOT.Modules.Games.Modules
{
    [Group("Games")]
    public class Minesweeper : InteractiveBase<ShardedCommandContext>
    {
        public Random Random {get;}

        public Minesweeper(Random random)
        {
            Random = random;
        }

        [Command("MinesweeperQuick")]
        [Summary("Generates a minesweeper game with revealed blanks")]
        public Task MineSweeperQuickAsync(int width = 10, int height = 10, int mine_count = 10)
        {
            return MinefieldGameAsync(width, height, mine_count, true);
        }

        public async Task<bool> IsGameAcceptable(int width, int height, int mine_count)
        {
            if (mine_count > width * height)
            {
                await ReplyAsync("Mine count must not be greater than amount of squares on board.");
                return false;
            }

            if (width <= 0 || height <= 0)
            {
                await ReplyAsync("Width and height must be greater than 0");
                return false;
            }

            if (mine_count <= 0)
            {
                await ReplyAsync("What fun would minesweeper be if there were no bombs.");
                return false;
            }

            return true;
        }

        public async Task MinefieldGameAsync(int width = 10, int height = 10, int mine_count = 10, bool quick = false)
        {
            try
            {
                if (!(await IsGameAcceptable(width, height, mine_count)))
                {
                    return;
                }

                int[,,] minefield = new int[width, height, 2];

                // 0 = none
                // -1 = mine
                // >0 = hints
                // Place each of the bombs.
                int minesPlaced = 0;
                while (minesPlaced < mine_count)
                {
                    int mine_x = Random.Next(width);
                    int mine_y = Random.Next(height);

                    if (minefield[mine_x, mine_y, 0] != -1)
                    {
                        minefield[mine_x, mine_y, 0] = -1;
                        minesPlaced++;
                    }
                }

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (minefield[x, y, 0] != -1)
                        {
                            minefield[x, y, 0] = MinesNear(minefield, width, height, y, x);
                        }
                    }
                }

                if (quick)
                {
                    minefield = RevealBlankSurrounds(minefield, width, height);
                }

                string fieldString = $"**Minesweeper {width}x{height} {mine_count}x:bomb:**\n";
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var fieldValue = minefield[x, y, 0];

                        // Check any squares that are revealed by default
                        if (minefield[x, y, 1] == 1)
                        {
                            fieldString += $":{minefieldItems[fieldValue + 1]}:";
                        }
                        else
                        {
                            fieldString += $"||:{minefieldItems[fieldValue + 1]}:||";
                        }
                    }

                    fieldString += "\n";
                }

                if (fieldString.Length > 2000)
                {
                    await ReplyAsync("Sorry, a minefield this big will exceed discords message length limit, please try a smaller board size.");
                    return;
                }

                await ReplyAsync(fieldString);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        [Command("Minesweeper")]
        [Summary("Generates a minesweeper game")]
        public Task MineSweeperAsync(int width = 10, int height = 10, int mine_count = 10)
        {
            return MinefieldGameAsync(width, height, mine_count, false);
        }

        public int[,,] RevealBlankSurrounds(int[,,] minefield, int width, int height)
        {
            // IIterate through each row and column
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Reveal each square that has no surrounding bombs.
                    if (minefield[x, y, 0] == 0)
                    {
                        minefield[x, y, 1] = 1;

                        // IIterate through each square that surrounds the blank one.
                        for (int bX = -1; bX <= 1; bX++)
                        {
                            for (int bY = -1; bY <= 1; bY++)
                            {
                                // Check to see if the square is in the bounds of the game
                                if (x + bX < width && y + bY < height && x + bX >= 0 && y + bY >= 0)
                                {
                                    if (minefield[x + bX, y + bY, 0] > 0)
                                    {
                                        minefield[x + bX, y + bY, 1] = 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return minefield;
        }

        private readonly List<string> minefieldItems = new List<string>
                                                 {
                                                     "bomb",
                                                     "black_large_square",
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
        
        public int MinesNear(int[,,] field, int width, int height, int y, int x)
        {
            int mines = 0;

            mines += MineAt(field, width, height, x - 1, y - 1); 
            mines += MineAt(field, width, height, x - 1, y); 
            mines += MineAt(field, width, height, x - 1, y + 1); 
            mines += MineAt(field, width, height, x, y - 1); 
            mines += MineAt(field, width, height, x, y + 1); 
            mines += MineAt(field, width, height, x + 1, y - 1); 
            mines += MineAt(field, width, height, x + 1, y); 
            mines += MineAt(field, width, height, x + 1, y + 1);

            return mines;
        }

        public bool IsInbounds(int width, int height, int x, int y)
        {
            if (x < width && x >= 0 && y < height && y >= 0)
            {
                return true;
            }

            return false;
        }

        public int MineAt(int[,,] field, int width, int height, int x, int y)
        {
            // we need to check also that we're not out of array bounds as that would
            // be an error
            if (y >= 0 && y < height && x >= 0 && x < width && field[x, y, 0] == -1) 
            {
                return 1;
            }

            return 0;
        }
    }
}