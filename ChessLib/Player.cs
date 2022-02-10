using System;
using Newtonsoft.Json;

namespace ChessLib
{
    /// <summary>
    /// A player
    /// </summary>
    public abstract class Player : IdObject
    {
        public Player(Game.Colors color, string name, int? elo)
            : base()
        {
            Color = color;
            Name = name;
            Elo = elo;
        }

        public string Name { get; set; }
        public int? Elo { get; set; }
        public Game.Colors Color { get; set; }
        public string DisplayName {
            get {
                if (Elo.HasValue)
                    return $"{Name} ({Elo})";
                return Name;
            }
        }
    } // Player

    /// <summary>
    /// An engine player
    /// </summary>
    public class EnginePlayer : Player
    {
        public EnginePlayer(Game.Colors color, string name, int? elo)
            : base(color, name, elo)
        {

        }

        public Engines.EngineBase Engine { get; set; }
        public Engines.TheKing.Personality Personality { get; set; }
        public string OpeningBookFileName {get; set; }

        [JsonIgnore]
        public Books.IBook OpeningBook { get; set; }
    } // EnginePlayer

    /// <summary>
    /// A human player
    /// </summary>
    public class HumanPlayer : Player
    {
        public HumanPlayer(Game.Colors color, string name, int? elo)
            : base(color, name, elo)
        {

        }
    } // HumanPlayer
}