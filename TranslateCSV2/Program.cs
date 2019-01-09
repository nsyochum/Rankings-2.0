using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TranslateCSV2
{
    class Program
    {
        public static readonly string PATH = "C:\\Users\\Nikolaus\\OneDrive\\Documents\\Code\\C#\\Rankings2\\data\\";

        static void Main(string[] args)
        {
            Console.WriteLine("What year?");
            string year = Console.ReadLine();

            List<FullGame> games = parseGames(PATH + year + "\\");
            Dictionary<string, Team> teams = teamParse(games);
            output(teams, PATH + year + "\\");
        }

        private static List<FullGame> parseGames(string path)
        {
            string file = "games.csv";
            string[] data = File.ReadAllLines(path + file);
            List<FullGame> games = new List<FullGame>();
            for (int i = 0; i < data.Length; i++)
            {
                string[] datum = data[i].Split(',');

                FullGame temp = new FullGame();
                temp.winner = datum[1];
                temp.loser = datum[3];
                temp.margin = int.Parse(datum[5]);

                games.Add(temp);
            }

            return games;
        }

        private static Dictionary<string, Team> teamParse(List<FullGame> games)
        {
            Dictionary<string, Team> teams = new Dictionary<string, Team>();
            foreach(FullGame game in games)
            {
                if(!teams.ContainsKey(game.winner))
                {
                    teams.Add(game.winner, new Team(game.winner));
                }

                if(!teams.ContainsKey(game.loser))
                {
                    teams.Add(game.loser, new Team(game.loser));
                }

                teams[game.winner].addWin(game.loser, game.margin);
                teams[game.loser].addLoss(game.winner, game.margin);
            }

            return teams;
        }

        private static void output(Dictionary<string, Team> teams, string path)
        {
            var output = new List<String>();
            output.Add("true");
            output.Add("<" + "unknown" + ">");
            foreach (var team in teams.Values)
            {
                if(team.Wins + team.Losses > 6)
                    output.Add(team.ToString());
            }
            output.Add("</" + "unkown" + ">");
            File.WriteAllLines(path + "teams.txt", output.ToArray());
        }
    }

    public struct FullGame
    {
        public string winner;
        public string loser;
        public int margin;
    }

}
