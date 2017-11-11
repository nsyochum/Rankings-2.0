using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Rankings2
{
    class Program
    {
        public static Dictionary<string, Team> teams = new Dictionary<string, Team>();
        public static Dictionary<string, List<RankedTeam>> conferences = new Dictionary<string, List<RankedTeam>>();
        public static SortedDictionary<double, Team> sos = new SortedDictionary<double, Team>();
        public static SortedDictionary<double, Team> rankings = new SortedDictionary<double, Team>();
        public static SortedDictionary<double, string> conferenceRankings = new SortedDictionary<double, string>();
        public static string DIRECTORY = "C:\\Users\\nikolaus\\OneDrive\\Documents\\Code\\C#\\Rankings2\\Rankings\\";
        public static string file = "rankings";
        private static double averageRating;
        private static double averageSOS;
        private static double averageWL;

        static void Main(string[] args)
        {
            Console.WriteLine("Week?");
            string week = Console.ReadLine();

            Console.WriteLine("College or Pro?");
            string level = Console.ReadLine();

            DIRECTORY += level + week + "\\";

            deserialize(args[0]);
            if (level.ToLower().StartsWith("c"))
            {
                setFCS();
            }
            else
            {
                file = "nfl";
            }
            rateTeams();
            rankSOS();
            rankConferences();
            output(level);
            outputCSV();
            Console.ReadLine();
        }

        private static void deserialize(string path)
        {
            var text = File.ReadAllLines(path);
            var conf = "";
            foreach (var token in text)
            {
                if (token == "true")
                {
                    continue;
                }

                if (token.StartsWith("</"))
                {
                    continue;
                }

                if (token.StartsWith("<"))
                {
                    var iter = token.Substring(1);
                    iter = iter.Substring(0, iter.Length - 1);
                    conf = iter;
                    conferences.Add(conf, new List<RankedTeam>());
                    continue;
                }

                Console.Write("*");
                Team team = Team.Deserialize(token);

                team.Conference = conf;
                teams.Add(team.University, team);
            }
        }

        /// <summary>
        /// Builds an entry in the teams dictionary for a single team that represents the record of all
        /// FCS teams against FBS teams.
        /// </summary>
        private static void setFCS()
        {
            var FCS = new Team("FCS");
            foreach (var team in teams)
            {
                foreach (var teamWin in team.Value.WinsList)
                {
                    if (!teams.ContainsKey(teamWin.name) && teamWin.name != "")
                    {
                        var game = new Game();
                        game.name = team.Value.University;
                        game.margin = teamWin.margin;
                        FCS.LossesList.Add(game);
                    }
                }

                foreach (var teamLoss in team.Value.LossesList)
                {
                    if (!teams.ContainsKey(teamLoss.name) && teamLoss.name != "")
                    {
                        var game = new Game();
                        game.name = team.Value.University;
                        game.margin = teamLoss.margin;
                        FCS.WinsList.Add(game);
                    }
                }
            }

            FCS.Conference = "FCS";
            teams.Add("FCS", FCS);
        }

        private static void rateTeams()
        {
            int loops = int.MaxValue;
            for (int i = 0; i < loops; i++)
            {
                Dictionary<string, double> tempRanks = new Dictionary<string, double>();
                double maxRating = double.MinValue;
                double minRating = double.MaxValue;
                foreach(Team team in teams.Values)
                {
                    double rate = team.getRating(teams);
                    tempRanks.Add(team.University, rate);

                    if(rate < minRating)
                    {
                        minRating = rate;
                    }
                    if (rate > maxRating)
                    {
                        maxRating = rate;
                    }
                }

                tempRanks = normalizeRates(minRating, maxRating, tempRanks);

                bool converged = true;
                foreach(string team in tempRanks.Keys)
                {
                    converged = converged && (Math.Abs(teams[team].CurrentRating - tempRanks[team]) < 0.0000000001);
                    teams[team].CurrentRating = tempRanks[team];
                }

                if(converged)
                {
                    Console.WriteLine("Number of loops: " + i);
                    loops = 0;
                }
            }

            foreach(Team team in teams.Values)
            {
                rankings.Add(team.CurrentRating, team);
            }
        }

        private static void rankSOS()
        {
            foreach (Team team in teams.Values)
            {
                List<double> sOs = new List<double>();
                foreach (var t in team.WinsList)
                {
                    Team win;
                    if (teams.TryGetValue(t.name, out win) || (teams.TryGetValue("FCS", out win) && t.name != ""))
                    {
                        sOs.Add(win.CurrentRating);
                    }
                }

                foreach (var t in team.LossesList)
                {
                    Team loss;
                    if (teams.TryGetValue(t.name, out loss) || (teams.TryGetValue("FCS", out loss) && t.name != ""))
                    {
                        sOs.Add(loss.CurrentRating);
                    }

                }
                
                double total = 0;
                double count = (double)sOs.Count;
                foreach (var wl in sOs)
                {
                    total += wl;
                }

                team.StrengthOfSchedule = total / count;
                while (true) {
                    try
                    {
                        sos.Add(team.StrengthOfSchedule, team);
                        break;
                    } catch (Exception e)
                    {
                        team.StrengthOfSchedule += 0.0000001;
                    }
                }

                if (team.University != "FCS")
                {
                    averageRating += team.CurrentRating;
                    averageSOS += team.StrengthOfSchedule;
                    averageWL += team.wL();
                }
            }

            int teamNum = teams.Count;
            averageRating /= teamNum;
            averageSOS /= teamNum;
            averageWL /= teamNum;
        }

        /// <summary>
        /// ranks the conferences
        /// </summary>
        private static void rankConferences()
        {
            foreach (var team in teams.Keys)
            {
                var t = new RankedTeam();
                t.name = teams[team].University;
                t.rating = teams[team].CurrentRating;
                if (conferences.ContainsKey(teams[team].Conference))
                    conferences[teams[team].Conference].Add(t);
            }

            foreach (var conf in conferences.Keys)
            {
                double aggregateRating = 0.0;
                double aggregateTotal = 0.0;
                int size = conferences[conf].Count;
                int nFact = factorial(size);
                for (int i = 0; i < size; i++)
                { 
                    // n!/(k!(n-k)!
                    int binomialFactor = (nFact) / (factorial(i)*factorial(size - i));
                    aggregateRating += conferences[conf].ElementAt(i).rating * binomialFactor;
                    aggregateTotal += binomialFactor;
                }

                double rating = aggregateRating / (aggregateTotal);

                conferenceRankings.Add(rating, conf);
            }
        }

        private static Dictionary<string, double> normalizeRates(double min, double max, Dictionary<string, double> ranks)
        {
            max = max - min;
            Dictionary<string, double> toReturn = new Dictionary<string, double>();
            foreach(string team in ranks.Keys)
            {
                double temp = ranks[team];
                temp -= min;
                temp /= max;
                toReturn.Add(team, temp);
            }
            return toReturn;
        }

        /// <summary>
        /// writes the data to an html file
        /// </summary>
        private static void output(string level)
        {
            Directory.CreateDirectory(DIRECTORY + "Teams");
            var outputData = new List<string>();
            outputData.Add("<!DOCTYPE html>");
            outputData.Add("<html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\">");
            outputData.Add("<head>\n<meta charset=\"utf-8\" />\n<title>" + level + " Football Rankings Date: " + System.DateTime.Today + "</title>\n</head>\n<body>\n" +
                "<h1 align = center>" + level + " Football Rankings</h1>\n\t<p align = center>Average Rating: " + averageRating + "</p>\n\t<p align = center>Average SoS: " + averageSOS + "</p>\n\t<p align = center>Average Win Percentage: " + averageWL + "</p>\n" +
                "<table align = 'center' border='1' cellspacing='0' cellpadding='4'>\t<caption><b>Team Ranks:</b></caption>\n");

            outputData.Add("<tr><td>Rank</td><td>Team</td><td>Wins</td><td>Losses</td><td>Rating</td><td>Conference</td></tr>");
            
            int rank = 1;
            foreach (var team in rankings.Reverse())
            {
                team.Value.setBestAndWorst(teams);
                outputData.Add("\t<tr>");
                outputData.Add("\t\t<td>" + rank + "</td>");
                outputData.Add("\t\t<td><a href=\"Teams/" + team.Value.University + ".html\">" + team.Value.University + "</a></td>"); // /nsyochum
                outputData.Add("\t\t<td>" + team.Value.Wins + "</td>");
                outputData.Add("\t\t<td>" + team.Value.Losses + "</td>");
                outputData.Add("\t\t<td>" + team.Value.CurrentRating + "</td>");
                outputData.Add("\t\t<td>" + team.Value.Conference + "</td>");
                outputData.Add("\t</tr>");
                team.Value.ToHTML(rank, DIRECTORY, teams);
                rank++;
            }

            outputData.Add("</table>\n<p></p>\n<table align = 'center' border='1' cellspacing='0' cellpadding='4'>\n\t<caption><b>Strength of Schedule Ranks:</b></caption>\n");
            outputData.Add("<tr><td>Rank</td><td>Team</td><td>Wins</td><td>Losses</td><td>Strength of Schedule</td><td>Conference</td></tr>");

            rank = 1;
            double worstSOS = sos.ToArray()[0].Key;
            double bestSOS = sos.ToArray()[sos.Count - 1].Key - worstSOS;
            foreach (var team in sos.Reverse())
            {
                outputData.Add("\t<tr>");
                outputData.Add("\t\t<td>" + rank + "</td>");
                outputData.Add("\t\t<td>" + team.Value.University + "</td>");
                outputData.Add("\t\t<td>" + team.Value.Wins + "</td>");
                outputData.Add("\t\t<td>" + team.Value.Losses + "</td>");
                outputData.Add("\t\t<td>" + team.Value.StrengthOfSchedule + "</td>");
                outputData.Add("\t\t<td>" + team.Value.Conference + "</td>");
                outputData.Add("\t</tr>");
                rank++;
            }

            outputData.Add("</table>\n<p></p>\n<table align = 'center' border='1' cellspacing='0' cellpadding='4'>\n\t<caption><b>Conference Ranks:</b></caption>\n");
            outputData.Add("<tr><td>Rank</td><td>Conference</td><td>Rating</td></tr>");

            rank = 1;
            foreach (var conf in conferenceRankings.Reverse())
            {
                outputData.Add("\t<tr>");
                outputData.Add("\t\t<td>" + rank + "</td>");
                outputData.Add("\t\t<td>" + conf.Value + "</td>");
                outputData.Add("\t\t<td>" + conf.Key + "</td>");
                outputData.Add("\t</tr>");
                rank++;
            }

            outputData.Add("</table>\n</body>\n</html>");

            Console.WriteLine();

            File.WriteAllLines(DIRECTORY + file + ".html", outputData.ToArray());

        }

        private static void outputCSV()
        {
            var outputData = new List<string>();
            outputData.Add("rank,team,winpercentage,rating,averageMOV,averageMOL,conference,rawRank");

            int rank = 1;
            foreach (var team in rankings.Reverse())
            {
                outputData.Add(rank + "," + team.Value.University + "," + team.Value.wL() + "," 
                        + team.Value.CurrentRating + "," + team.Value.averageMarginOfVictory() + "," + team.Value.averageMarginOfLoss() + "," + team.Value.Conference + "," + team.Value.getRating(teams));
                rank++;
            }

            File.WriteAllLines(DIRECTORY + file + ".csv", outputData.ToArray());
        }

        private static int factorial(int n)
        {
            if(n == 0)
            {
                return 1;
            } else
            {
                return n * factorial(n - 1);
            }
        }
    }
    public struct RankedTeam
    {
        public string name;
        public int rank;
        public double rating;
    }
}
