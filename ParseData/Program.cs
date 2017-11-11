using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ParseData
{
    class Program
    {
        public static string CWD = "C:\\Users\\nikolaus\\OneDrive\\Documents\\Code\\C#\\Rankings2\\ParseData";
        private static Dictionary<string, Team> teams;
        private static Dictionary<string, Dictionary<string, Team>> conferences;

        static void Main(string[] args)
        {
            Console.WriteLine("NFL or College?");
            string level = Console.ReadLine();
            level = level[0] == 'N' ? "NFL" : "";
            CWD = args[0];
            conferences = new Dictionary<string, Dictionary<string, Team>>();
            teams = getTeams(level);
            getGames(level);
            getConferences();
            output(CWD + "\\" + level + "teams.txt");
        }

        private static Dictionary<string, Team> getTeams(string level)
        {
            Dictionary<string, Team> teams = new Dictionary<string, Team>();
            string[] teamNames = File.ReadAllLines(CWD + "\\" + level + "teams.csv");
            for(int i = 0; i < teamNames.Length; i++)
            {
                string teamName = teamNames[i].Split(',')[1].Trim();
                string conference = teamNames[i].Split(',')[2].Trim();
                var team = new Team(teamName);
                team.Conference = conference;
                teams.Add(teamName, team);
                if(!conferences.ContainsKey(conference))
                {
                    conferences.Add(conference, new Dictionary<string, Team>());
                }
            }
            if (level.Length < 1)
                teams.Add("FCS", new Team("FCS"));

            return teams;
        }

        private static void getGames(string level)
        {
            string[] rawGames = File.ReadAllLines(CWD + "\\" + level + "games.csv");
            Dictionary<string, Game> allGames = new Dictionary<string, Game>();
            foreach (var game in rawGames)
            {
                // date, team 1, score1, team2, score2
                string[] data = game.Split(',');
                int margin = int.Parse(data[2]) - int.Parse(data[4]);
                var winner = data[1].Trim(new char[] { '@' });
                var winTeam = teams.ContainsKey(winner) ? teams[winner] : teams["FCS"];

                // if not a neutral site game
                if (data[1].StartsWith("@") || data[3].StartsWith("@"))
                {
                    string hometeam = (data[1].StartsWith("@")) ? data[1] : data[3];
                    string awayteam = (data[1].StartsWith("@")) ? data[3] : data[1];
                    hometeam = hometeam.Substring(1);
                    var awayGame = new Game(margin, hometeam, GameLocation.AWAY);
                    var homeGame = new Game(margin, awayteam, GameLocation.HOME);
                    
                    // home win
                    if(hometeam.Equals(winner))
                    {
                        var loseTeam = teams.ContainsKey(awayteam) ? teams[awayteam] : teams["FCS"];
                        winTeam.WinsList.Add(homeGame);
                        loseTeam.LossesList.Add(awayGame);
                    }
                    // away win
                    else
                    {
                        var loseTeam = teams.ContainsKey(hometeam) ? teams[hometeam] : teams["FCS"];
                        winTeam.WinsList.Add(awayGame);
                        loseTeam.LossesList.Add(homeGame);
                    }
                }
                else
                {
                    string loser = data[3];
                    var winGame = new Game(margin, loser, GameLocation.NEUTRAL);
                    var loseGame = new Game(margin, winner, GameLocation.NEUTRAL);

                    var loseTeam = teams.ContainsKey(loser) ? teams[loser] : teams["FCS"];
                    winTeam.WinsList.Add(winGame);
                    loseTeam.LossesList.Add(loseGame);
                }

            }
        }

        private static void getConferences()
        {
            foreach(var team in teams.Values)
            {
                if (!team.University.Equals("FCS"))
                {
                    conferences[team.Conference].Add(team.University, team);
                }
            }
        }

        private static void output(string file)
        {
            var output = new List<String>();

            foreach (var conference in conferences)
            {
                output.Add("<" + conference.Key + ">");
                foreach (var team in conference.Value)
                {
                    output.Add(team.Value.ToString());
                }
                output.Add("</" + conference.Key + ">");
            }
            File.WriteAllLines(file, output.ToArray());
        }
    }
}
