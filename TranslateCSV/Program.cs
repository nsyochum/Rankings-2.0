using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TranslateCSV
{
    class Program
    {
        public static readonly string PATH = "C:\\Users\\Nikolaus\\OneDrive\\Documents\\Code\\C#\\Rankings2\\data\\";
        static void Main(string[] args)
        {
            Console.WriteLine("What year?");
            //int year = int.Parse(Console.ReadLine());

            for (int year = 2005; year <= 2013; year++)
            {
                string curPath = PATH + year + "\\";
                Dictionary<int, string> confKeys = parseConferences(curPath);
                Dictionary<int, SmallTeam> teamKeys = parseTeams(curPath);
                Dictionary<long, FullGame> gameKeys = parseGames(curPath);
                Dictionary<int, Team> teams = makeTeams(teamKeys, gameKeys);
                Dictionary<string, List<Team>> conferences = makeConferences(teams, confKeys);
                output(conferences, curPath);
            }
        }

        private static Dictionary<int, string> parseConferences(string path)
        {
            Dictionary<int, string> conferences = new Dictionary<int, string>();
            string file = "conference.csv";
            string[] data = File.ReadAllLines(path + file);
            for (int i = 1; i < data.Length; i++)
            {
                string[] datum = data[i].Split(',');
                if (datum[2].Equals("\"FBS\""))
                {
                    conferences.Add(int.Parse(datum[0]), datum[1].Trim(new char[] { '\"' }));
                }
            }
            return conferences;
        }

        private static Dictionary<int, SmallTeam> parseTeams(string path)
        {
            Dictionary<int, SmallTeam> teams = new Dictionary<int, SmallTeam>();
            string file = "team.csv";
            string[] data = File.ReadAllLines(path + file);
            for(int i =1; i < data.Length; i++)
            {
                string[] datum = data[i].Split(',');
                SmallTeam current = new SmallTeam();
                current.code = int.Parse(datum[0]);
                current.team = datum[1].Trim(new char[] { '\"' });
                current.conference = int.Parse(datum[2]);

                teams.Add(current.code, current);
            }
            return teams;
        }

        private static Dictionary<long, FullGame> parseGames(string path)
        {
            var games = new Dictionary<long, FullGame>();
            string file = "team-game-statistics.csv";
            string[] data = File.ReadAllLines(path + file);
            var gameLoc = loadGames(path);

            for (int i = 1; i < data.Length; i++)
            {
                string[] datum = data[i].Split(',');
                long code = long.Parse(datum[1]);
                if (games.ContainsKey(code))
                {
                    FullGame current = games[code];
                    int secondTeam = int.Parse(datum[0]);
                    int secondScore = int.Parse(datum[35]);
                    if(current.margin > secondScore)
                    {
                        current.margin -= secondScore;
                        current.loser = secondTeam;
                    } else
                    {
                        current.margin = secondScore - current.margin;
                        current.loser = current.winner;
                        current.winner = secondTeam;
                    }

                    current.homeTeam = (gameLoc[code].neutralSite) ? (1) : ((gameLoc[code].home == current.winner) ? (0) : (2));

                    games.Remove(code);
                    games.Add(code, current);
                } else
                {
                    FullGame current = new FullGame();
                    current.winner = int.Parse(datum[0]);
                    current.code = code;
                    current.margin = int.Parse(datum[35]);

                    games.Add(code, current);
                }
            }

            return games;
        }
        
        private static Dictionary<int, Team> makeTeams(Dictionary<int, SmallTeam> justTeams, Dictionary<long, FullGame> games)
        {
            Dictionary<int, Team> teams = new Dictionary<int, Team>();
            foreach(SmallTeam team in justTeams.Values)
            {
                Team newTeam = new Team(team.team);
                newTeam.Conference = "" + team.conference;
                teams.Add(team.code, newTeam);
            }

            foreach(FullGame game in games.Values)
            {
                Team winner = teams[game.winner];
                Team loser = teams[game.loser];
                // 0 win, 1 neutral, 2 loser
                GameLocation winLoc;
                GameLocation lossLoc;
                switch(game.homeTeam)
                {
                    case 1:
                        winLoc = GameLocation.NEUTRAL;
                        lossLoc = GameLocation.NEUTRAL;
                    break;
                    case 0:
                        winLoc = GameLocation.HOME;
                        lossLoc = GameLocation.AWAY;
                    break;
                    default:
                        winLoc = GameLocation.AWAY;
                        lossLoc = GameLocation.HOME;
                    break;
                }
                winner.WinsList.Add(new Game(game.margin, loser.University, winLoc));
                loser.LossesList.Add(new Game(game.margin, winner.University, lossLoc));
            }

            return teams;
        }

        private static Dictionary<string, List<Team>> makeConferences(Dictionary<int, Team> teams, Dictionary<int, string> conferences)
        {
            Dictionary<string, List<Team>> finalConf = new Dictionary<string, List<Team>>();
            foreach(Team team in teams.Values)
            {
                int code = int.Parse(team.Conference);
                if (conferences.ContainsKey(code))
                {
                    team.Conference = conferences[code];
                    if (!finalConf.ContainsKey(conferences[code]))
                    {
                        finalConf.Add(conferences[code], new List<Team>());
                    }

                    finalConf[conferences[code]].Add(team);
                }
            }

            return finalConf;
        }

        private static void output(Dictionary<string, List<Team>> teams, string path)
        {
            var output = new List<String>();
            output.Add("true");
            foreach (var conference in teams)
            {
                output.Add("<" + conference.Key + ">");
                foreach (var team in conference.Value)
                {
                    output.Add(team.ToString());
                }
                output.Add("</" + conference.Key + ">");
            }
            File.WriteAllLines(path + "teams.txt", output.ToArray());
        }

        private static Dictionary<long, SmallGame> loadGames(string path)
        {
            string file = "game.csv";
            var games = new Dictionary<long, SmallGame>();
            string[] data = File.ReadAllLines(path + file);
            for (int i = 1; i < data.Length; i++)
            {
                var game = new SmallGame();
                string[] datum = data[i].Split(',');
                game.code = long.Parse(datum[0]);
                game.home = int.Parse(datum[3]);
                game.visit = int.Parse(datum[2]);
                game.neutralSite = datum[5].Equals("NEUTRAL");

                games.Add(game.code, game);
            }

            return games;
        }
    }

    public struct SmallTeam
    {
        public int code;
        public string team;
        public int conference;
    }

    public struct FullGame
    {
        public int winner;
        public int loser;
        public long code;
        public int margin;
        public int homeTeam; // 0 win, 1 neutral, 2 loser
    }

    public struct SmallGame
    {
        public long code;
        public int visit;
        public int home;
        public bool neutralSite;
    }
}
