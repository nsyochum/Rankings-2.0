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
        public static Dictionary<string, SortedList<double, RankedTeam>> conferences = new Dictionary<string, SortedList<double, RankedTeam>>();
        public static SortedDictionary<double, Team> sos = new SortedDictionary<double, Team>();
        public static SortedDictionary<double, Team> rankings = new SortedDictionary<double, Team>();
        public static SortedDictionary<double, string> conferenceRankings = new SortedDictionary<double, string>();
        public static string DIRECTORY = "C:\\Users\\nsyoc\\OneDrive\\Documents\\Code\\C#\\Rankings2\\Rankings\\";
        public static string file = "rankings";
        private static double averageRating;
        private static double averageSOS;
        private static double averageWL;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("Week?");
            var week = Console.ReadLine();

            Console.WriteLine("College or Pro?");
            var level = Console.ReadLine();

            var otherLevel = "";

            DIRECTORY += level + week + "\\";

            deserialize(args[0]);
            if (level.ToLower().StartsWith("c"))
            {
                otherLevel = "FCS";
                setOther("FCS");
            }
            else if (level.ToLower().StartsWith("f"))
            {
                otherLevel = "FBS";
                setOther("FBS");
                file = "FCS";
            }
            else
            {
                file = "nfl";
            }
            rateTeams(otherLevel);
            rankSOS(otherLevel);
            rankConferences();
            output(level, otherLevel);
            outputCSV(otherLevel);
            Console.ReadLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
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
                    conferences.Add(conf, new SortedList<double, RankedTeam>());
                    continue;
                }

                Console.Write("*");
                var team = Team.Deserialize(token);

                team.Conference = conf;
                teams.Add(team.University, team);
            }
        }

        /// <summary>
        /// Builds an entry in the teams dictionary for a single team that represents the record of all
        /// FCS teams against FBS teams.
        /// </summary>
        private static void setOther(string otherLevel)
        {
            var other = new Team(otherLevel);
            foreach (var team in teams)
            {
                foreach (var teamWin in team.Value.WinsList)
                {
                    if (!teams.ContainsKey(teamWin.name) && teamWin.name != "")
                    {
                        var game = new Game();
                        game.name = team.Value.University;
                        game.margin = teamWin.margin;
                        other.LossesList.Add(game);
                    }
                }

                foreach (var teamLoss in team.Value.LossesList)
                {
                    if (!teams.ContainsKey(teamLoss.name) && teamLoss.name != "")
                    {
                        var game = new Game();
                        game.name = team.Value.University;
                        game.margin = teamLoss.margin;
                        other.WinsList.Add(game);
                    }
                }
            }

            other.Conference = otherLevel;
            teams.Add(otherLevel, other);
        }

        /// <summary>
        /// 
        /// </summary>
        private static void rateTeams(string otherLevel)
        {
            int loops = 10000;//int.MaxValue;
            for (var i = 0; i < loops; i++)
            {
                Dictionary<string, double> tempRanks = new Dictionary<string, double>();
                double maxRating = double.MinValue;
                double minRating = double.MaxValue;
                foreach(var team in teams.Values)
                {
                    var rate = team.getRating(teams, otherLevel);
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

                var converged = true;
                foreach(var team in tempRanks.Keys)
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

            foreach(var team in teams.Values)
            {
                Console.WriteLine(team.CurrentRating);
                rankings.Add(team.CurrentRating, team);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void rankSOS(string otherLevel)
        {
            foreach (Team team in teams.Values)
            {
                List<double> sOs = new List<double>();
                foreach (var t in team.WinsList)
                {
                    Team win;
                    if (teams.TryGetValue(t.name, out win) || (teams.TryGetValue(otherLevel, out win) && t.name != ""))
                    {
                        sOs.Add(win.CurrentRating);
                    }
                }

                foreach (var t in team.LossesList)
                {
                    Team loss;
                    if (teams.TryGetValue(t.name, out loss) || (teams.TryGetValue(otherLevel, out loss) && t.name != ""))
                    {
                        sOs.Add(loss.CurrentRating);
                    }

                }
                
                var total = 0.0;
                var count = (double)sOs.Count;
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

                if (team.University != otherLevel)
                {
                    averageRating += team.CurrentRating;
                    averageSOS += team.StrengthOfSchedule;
                    averageWL += team.wL();
                }
            }

            var teamNum = teams.Count;
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
                    conferences[teams[team].Conference].Add(t.rating, t);
            }

            foreach (var conf in conferences.Keys)
            {
                //sort first
                var tempConf = new List<RankedTeam>();
                var aggregateRating = 0.0;
                var aggregateTotal = 0.0;
                var size = conferences[conf].Count;
                Console.WriteLine(conf);
                Console.WriteLine(size);
                var nFact = factorial(size - 1);
                for (var i = 0; i < size; i++)
                { 
                    // n!/(k!(n-k)!
                    int binomialFactor = (nFact) / (factorial(i)*factorial(size - i - 1));
                    aggregateRating += conferences[conf].ElementAt(i).Value.rating * binomialFactor;
                    aggregateTotal += binomialFactor;
                    Console.WriteLine(conferences[conf].ElementAt(i).Value.name + "|" + conferences[conf].ElementAt(i).Value.rating + "|" + binomialFactor + "|" + conferences[conf].ElementAt(i).Value.rating * binomialFactor);
                }

                Console.WriteLine();
                var rating = aggregateRating / (aggregateTotal);
                Console.WriteLine(aggregateRating + "|" + aggregateTotal + "|" + rating);

                conferenceRankings.Add(rating, conf);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Takes the current ratings of everey team and scales them between 0 and 1
        /// </summary>
        /// <param name="min">the smallest rating before this loop</param>
        /// <param name="max">the largest rating before this loop</param>
        /// <param name="ranks">dictionary containing the teams and their current ranks</param>
        /// <returns></returns>
        private static Dictionary<string, double> normalizeRates(double min, double max, Dictionary<string, double> ranks)
        {
            max = max - min;
            Dictionary<string, double> toReturn = new Dictionary<string, double>();
            foreach(var team in ranks.Keys)
            {
                var temp = ranks[team];
                temp -= min;
                temp /= max;
                if(double.IsNaN(temp))
                {
                    Console.WriteLine(team);
                }
                toReturn.Add(team, temp);
            }
            return toReturn;
        }

        /// <summary>
        /// writes the data to an html file
        /// </summary>
        private static void output(string level,string otherLevel)
        {
            Directory.CreateDirectory(DIRECTORY + "Teams");
            var outputData = new List<string>();
            outputData.Add("<!DOCTYPE html>");
            outputData.Add("<html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\">");
            outputData.Add("<head>\n<meta charset=\"utf-8\" />\n<title>" + level + " Football Rankings Date: " + System.DateTime.Today + "</title>\n</head>\n<body>\n" +
                "<h1 align = center>" + level + " Football Rankings</h1>\n\t<p align = center>Average Rating: " + averageRating + "</p>\n\t<p align = center>Average SoS: " + averageSOS + "</p>\n\t<p align = center>Average Win Percentage: " + averageWL + "</p>\n" +
                "<table align = 'center' border='1' cellspacing='0' cellpadding='4'>\t<caption><b>Team Ranks:</b></caption>\n");

            outputData.Add("<tr><td>Rank</td><td>Team</td><td>Wins</td><td>Losses</td><td>Rating</td><td>Conference</td></tr>");
            
            var rank = 1;
            foreach (var team in rankings.Reverse())
            {
                team.Value.setBestAndWorst(teams, otherLevel);
                outputData.Add("\t<tr>");
                outputData.Add("\t\t<td>" + rank + "</td>");
                outputData.Add("\t\t<td><a href=\"Teams/" + team.Value.University + ".html\">" + team.Value.University + "</a></td>"); // <IMG src=\"Teams/" + team.Value.University +".bmp\" height=\"20\" width=\"20\"
                outputData.Add("\t\t<td>" + team.Value.Wins + "</td>");
                outputData.Add("\t\t<td>" + team.Value.Losses + "</td>");
                outputData.Add("\t\t<td>" + team.Value.CurrentRating + "</td>");
                outputData.Add("\t\t<td>" + team.Value.Conference + "</td>");
                outputData.Add("\t</tr>");
                team.Value.ToHTML(rank, DIRECTORY, teams, otherLevel);
                rank++;
            }

            outputData.Add("</table>\n<p></p>\n<table align = 'center' border='1' cellspacing='0' cellpadding='4'>\n\t<caption><b>Strength of Schedule Ranks:</b></caption>\n");
            outputData.Add("<tr><td>Rank</td><td>Team</td><td>Wins</td><td>Losses</td><td>Strength of Schedule</td><td>Conference</td></tr>");

            rank = 1;
            var worstSOS = sos.ToArray()[0].Key;
            var bestSOS = sos.ToArray()[sos.Count - 1].Key - worstSOS;
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

        private static void outputCSV(string otherLevel)
        {
            var outputData = new List<string>();
            outputData.Add("rank,team,winpercentage,rating,averageMOV,averageMOL,conference,rawRank");

            var rank = 1;
            foreach (var team in rankings.Reverse())
            {
                outputData.Add(rank + "," + team.Value.University + "," + team.Value.wL() + "," 
                        + team.Value.CurrentRating + "," + team.Value.averageMarginOfVictory() + "," + team.Value.averageMarginOfLoss() + "," + team.Value.Conference + "," + team.Value.getRating(teams, otherLevel));

                team.Value.ToCSV(rank, DIRECTORY, teams, otherLevel);
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
