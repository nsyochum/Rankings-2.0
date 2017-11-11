using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ParseData
{
    public class Team
    {
        private string university;
        private List<Game> listOfLosses;
        private List<Game> listOfWins;
        private string conference;
        private double sos;
        private Team bestWin;
        private Team worstLoss;

        /// <summary>
        /// constructs a team object
        /// </summary>
        /// <param name="name">
        /// The name of the university
        /// </param>
        public Team(string name)
        {
            this.university = name;
            this.listOfLosses = new List<Game>();
            this.listOfWins = new List<Game>();
            this.bestWin = null;
            this.worstLoss = null;
            this.CurrentRating = 0;
        }

        /// <summary>
        /// calculates the win percentage of the team
        /// </summary>
        /// <returns>
        /// the win percentage
        /// </returns>
        public double wL()
        {
            if (this.Losses + this.Wins == 0)
                return 0;

            return ((double)this.Wins) / ((double)(this.Losses + this.Wins));
        }

        /// <summary>
        /// Calculates a rating for a team
        /// </summary>
        /// <returns>The rating for the team</returns>
        public double getRating(Dictionary<string, Team> teams)
        {
            double winsRank = 0;
            double lossesRank = 0;
            List<Game> adjWins = new List<Game>();
            List<Game> adjLoss = new List<Game>();
            foreach (var game in this.WinsList)
            {
                var newGame = Game.Clone(game);
                if(game.location == GameLocation.HOME)
                {
                    newGame.margin -= 3;
                    if (newGame.margin < 0)
                    {
                        newGame.margin = Math.Abs(newGame.margin);
                        adjLoss.Add(newGame);
                        continue;
                    }
                } 
                else if(game.location == GameLocation.AWAY)
                {
                    newGame.margin += 3;
                }
                adjWins.Add(newGame);
            }

            foreach (var game in this.LossesList)
            {
                var newGame = Game.Clone(game);
                if (game.location == GameLocation.AWAY)
                {
                    newGame.margin -= 3;
                    if(game.margin < 0)
                    {
                        newGame.margin = Math.Abs(newGame.margin);
                        adjWins.Add(newGame);
                        continue;
                    }
                }
                else if (game.location == GameLocation.HOME)
                {
                    newGame.margin += 3;
                }
                adjLoss.Add(newGame);
            }

            foreach (var game in adjWins)
            {
                Team win;
                double margin = Math.Log(game.margin + 1);
                

                if (teams.TryGetValue(game.name, out win))
                {
                    winsRank += margin * win.CurrentRating;
                }
                else if (game.name != "")
                {
                    win = teams["FCS"];
                    winsRank += margin * win.CurrentRating;
                }
            }

            foreach (var game in adjLoss)
            {
                Team loss;
                double margin = Math.Log(game.margin + 1);
                //double margin = 1;

                if (teams.TryGetValue(game.name, out loss))
                {
                    lossesRank += (1 - loss.CurrentRating) * margin;
                }
                else if (game.name != "")
                {
                    loss = teams["FCS"];
                    lossesRank += (1 - loss.CurrentRating) * margin;
                }
            }
            return (winsRank - lossesRank) / (this.Wins + this.Losses);
        }

        /// <summary>
        /// Calculates the average margin of victory
        /// </summary>
        /// <returns>the average margin of victory</returns>
        public double averageMarginOfVictory()
        {
            double score = 0;
            foreach (var game in this.WinsList)
            {
                score += (double)game.margin;
            }
            if (this.Wins != 0)
                return score / (double)this.Wins;

            return 0;
        }

        /// <summary>
        /// calculates the average margin of defeat
        /// </summary>
        /// <returns>average margin of defeat</returns>
        public double averageMarginOfLoss()
        {
            double score = 0;
            foreach (var game in this.LossesList)
            {
                score += (double)game.margin;
            }
            if (this.Losses != 0)
                return score / (double)this.Losses;

            return 0;
        }

        /// <summary>
        /// Number of wins
        /// </summary>
        public int Wins
        {
            get
            {
                return this.WinsList.Count;
            }
        }

        /// <summary>
        /// List of losses
        /// </summary>
        public List<Game> LossesList
        {
            get
            {
                return this.listOfLosses;
            }
            set
            {
                this.listOfLosses = value;
            }
        }

        /// <summary>
        /// List of wins
        /// </summary>
        public List<Game> WinsList
        {
            get
            {
                return this.listOfWins;
            }

            set
            {
                this.listOfWins = value;
            }
        }

        /// <summary>
        /// number of losses
        /// </summary>
        public int Losses
        {
            get
            {
                return this.LossesList.Count;
            }
        }

        /// <summary>
        /// Name of the team
        /// </summary>
        public string University
        {
            get
            {
                return this.university;
            }
            set
            {
                this.university = value;
            }
        }

        /// <summary>
        /// conference the team is in
        /// </summary>
        public string Conference
        {
            get
            {
                return this.conference;
            }
            set
            {
                this.conference = value;
            }
        }

        /// <summary>
        /// the strength of schedule of the team
        /// </summary>
        public double StrengthOfSchedule
        {
            get
            {
                return this.sos;
            }
            set
            {
                this.sos = value;
            }
        }

        public double CurrentRating
        {
            get;
            set;
        }


        /// <summary>
        /// returns a string representation of the Team
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string returnStringLosses = "";
            foreach (var team in this.LossesList)
            {
                returnStringLosses += team.ToString() + "/";
            }

            string returnStringWins = "";
            foreach (var team in this.WinsList)
            {
                returnStringWins += team.ToString() + "/";
            }
            if (returnStringWins.Length != 0)
                returnStringWins = returnStringWins.Substring(0, returnStringWins.Length - 1);

            if (returnStringLosses.Length != 0)
                returnStringLosses = returnStringLosses.Substring(0, returnStringLosses.Length - 1);

            return this.University + "," + this.Wins + "," + this.Losses + "," + returnStringWins + "^" + returnStringLosses;
        }

        /// <summary>
        /// Turns a properly formated string into a Team
        /// </summary>
        /// <param name="token">A properly formated String</param>
        /// <returns>The Team object</returns>
        public static Team Deserialize(string token)
        {
            var tokens = token.Split(',');
            var team = new Team(tokens[0]);
            var lists = tokens[3].Split('^');
            var wins = lists[0].Split('/');
            var losses = lists[1].Split('/');
            foreach (var win in wins)
            {
                if (win != "")
                    team.WinsList.Add(Game.Deserialize(win));
            }

            foreach (var loss in losses)
            {
                if (loss != "")
                    team.LossesList.Add(Game.Deserialize(loss));
            }

            return team;
        }

        /// <summary>
        /// sets the best win and worst loss of the team
        /// </summary>
        public void setBestAndWorst(Dictionary<string, Team> teams)
        {
            Team temp;
            string winString, lossString;

            if (this.Wins != 0)
            {
                foreach (var team in this.WinsList)
                {
                    if (teams.TryGetValue(team.name, out temp) && bestWin != null)
                    {
                        if (temp.getRating(teams) > bestWin.getRating(teams))
                        {
                            bestWin = temp;
                        }
                    }
                    else if (teams.TryGetValue("FCS", out temp) && bestWin != null)
                    {
                        if (temp.getRating(teams) > bestWin.getRating(teams))
                        {
                            bestWin = temp;
                        }
                    }
                    else if (teams.TryGetValue(team.name, out temp))
                    {
                        bestWin = temp;
                    }
                    else if (teams.TryGetValue("FCS", out temp) && team.name != "")
                    {
                        bestWin = temp;
                    }
                }
                winString = bestWin.University;
            }
            else
            {
                winString = "No wins";
            }

            if (this.Losses != 0)
            {
                foreach (var team in this.LossesList)
                {
                    if (teams.TryGetValue(team.name, out temp) && worstLoss != null)
                    {
                        if (temp.getRating(teams) < worstLoss.getRating(teams))
                        {
                            worstLoss = temp;
                        }
                    }
                    else if (teams.TryGetValue("FCS", out temp) && worstLoss != null)
                    {
                        if (temp.getRating(teams) < worstLoss.getRating(teams))
                        {
                            worstLoss = temp;
                        }
                    }
                    else if (teams.TryGetValue(team.name, out temp))
                    {
                        worstLoss = temp;
                    }
                    else if (teams.TryGetValue("FCS", out temp) && team.name != "")
                    {
                        worstLoss = temp;
                    }
                }
                lossString = worstLoss.University;
            }
            else
            {
                lossString = "Undefeated";
            }
        }

        /// <summary>
        /// Writes an HTML representation of a team to a file
        /// </summary>
        /// <param name="rank">Rank of the team</param>
        /// <param name="path">Path to write the team to</param>
        public void ToHTML(int rank, string path, Dictionary<string, Team> teams)
        {
            string winString, lossString;

            if (this.Wins != 0)
            {
                winString = bestWin.University;
            }
            else
            {
                winString = "No wins";
            }

            if (this.Losses != 0)
            {
                lossString = worstLoss.University;
            }
            else
            {
                lossString = "Undefeated";
            }

            var outputData = new List<string>();
            outputData.Add("<!DOCTYPE html>");
            outputData.Add("<html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\">");
            outputData.Add("<head>\n<meta charset=\"utf-8\" />\n<title>" + this.University + "</title>\n</head>\n<body>\n<h1 align = center>" + this.University + "\tRank: " + rank + "\tW-L: " + this.Wins + "-" + this.Losses + "</h1>\n<p align = center>Rating: " + this.CurrentRating + "</p>\n<p align = center>Conference: " + this.Conference + "</p>\n<p align = center>Best Win: " + winString + "\tWorst Loss: " + lossString + "</p>\n<p align = center>Average Margin of Victory: " + (int)averageMarginOfVictory() + "\tAverage Margin of Loss: " + (int)averageMarginOfLoss() + "</p>\n<table align = 'center' border='1' cellspacing='0' cellpadding='4'>\t<caption><b>SoS: " + sos + "</b></caption>\n");
            outputData.Add("<tr><td>Team</td><td>W-L</td><td>Team Rating</td><td>Result</td><td>Margin</td></tr>");

            foreach (var winName in this.WinsList)
            {
                Team win;
                if (teams.TryGetValue(winName.name, out win))
                {
                    outputData.Add("\t<tr>");
                    outputData.Add("\t\t<td><a href=\"" + win.University + ".html\" > " + win.University + "</a></td>");
                    outputData.Add("\t\t<td>" + win.Wins + "-" + win.Losses + "</td>");
                    outputData.Add("\t\t<td>" + win.CurrentRating + "</td>");
                    outputData.Add("\t\t<td>Win</td>");
                    outputData.Add("\t\t<td>" + winName.margin + "</td>");
                    outputData.Add("\t</tr>");
                }
                else if (teams.TryGetValue("FCS", out win) && winName.name != "")
                {
                    outputData.Add("\t<tr>");
                    outputData.Add("\t\t<td>" + winName.name + "</td>");
                    outputData.Add("\t\t<td>" + "UNKNOWN" + "</td>");
                    outputData.Add("\t\t<td>" + win.CurrentRating + "</td>");
                    outputData.Add("\t\t<td>Win</td>");
                    outputData.Add("\t\t<td>" + winName.margin + "</td>");
                    outputData.Add("\t</tr>");
                }
            }

            foreach (var lossName in this.LossesList)
            {
                Team loss;
                if (teams.TryGetValue(lossName.name, out loss))
                {
                    outputData.Add("\t<tr>");
                    outputData.Add("\t\t<td><a href=\"" + loss.University + ".html\">" + loss.University + "</a></td>");
                    outputData.Add("\t\t<td>" + loss.Wins + "-" + loss.Losses + "</td>");
                    outputData.Add("\t\t<td>" + loss.CurrentRating + "</td>");
                    outputData.Add("\t\t<td>Loss</td>");
                    outputData.Add("\t\t<td>" + lossName.margin + "</td>");
                    outputData.Add("\t</tr>");
                }
                else if (teams.TryGetValue("FCS", out loss) && lossName.name != "")
                {
                    outputData.Add("\t<tr>");
                    outputData.Add("\t\t<td>" + lossName.name + "</td>");
                    outputData.Add("\t\t<td>" + "UNKNOWN" + "</td>");
                    outputData.Add("\t\t<td>" + loss.CurrentRating + "</td>");
                    outputData.Add("\t\t<td>Loss</td>");
                    outputData.Add("\t\t<td>" + lossName.margin + "</td>");
                    outputData.Add("\t</tr>");
                }
            }

            outputData.Add("<t/table>\n</body>\n</html>");
            File.WriteAllLines(path + "Teams/" + this.University + ".html", outputData.ToArray());
        }
    }
}
