using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslateCSV
{
    public class Game
    {
        public int margin;
        public string name;
        // 0 home, 1 neutral, 2 away
        public GameLocation location;

        public Game()
        {
            margin = 0;
            name = "";
            location = GameLocation.NEUTRAL;
        }

        public Game(int margin, string name, GameLocation location)
        {
            if (margin < 0)
            {
                throw new ArgumentOutOfRangeException("Margin", "margin must be nonnegative");
            }
            this.margin = margin;
            this.name = name;
            this.location = location;
        }

        public override string ToString()
        {
            return name + "*" + margin + "*" + (int)location;
        }

        public static Game Deserialize(string data)
        {
            var game = new Game();
            var dataArr = data.Split('*');
            game.name = dataArr[0];
            game.margin = int.Parse(dataArr[1]);
            game.location = (GameLocation)int.Parse(dataArr[2]);
            return game;
        }

        public static Game Clone(Game game)
        {
            var newGame = new Game();
            newGame.name = game.name;
            newGame.margin = game.margin;
            newGame.location = game.location;
            return newGame;
        }
    }

    public enum GameLocation
    {
        HOME, NEUTRAL, AWAY

    }
}
