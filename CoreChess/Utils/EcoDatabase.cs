using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CoreChess.Utils
{
    public class EcoDatabase
    {
        public class Eco
        {
            public string Code { get; set; }
            public string Moves { get; set; }
            public string Name { get; set; }
            public string Variation { get; set; }

            public string FullName
            {
                get
                {
                    if (string.IsNullOrEmpty(Variation))
                        return $"{Code}: {Name}";
                    return $"{Code}: {Name}, {Variation}";
                }
            }
        } // Eco

        private Dictionary<string, Eco> m_EcoDatabaseByMoves = new Dictionary<string, Eco>();

        public EcoDatabase()
        {
        }

        public Eco GetByMoves(string moves)
        {
            Eco foundEco;
            moves = moves.Trim();
            if (!string.IsNullOrEmpty(moves) && m_EcoDatabaseByMoves.TryGetValue(moves, out foundEco))
                return foundEco;
            return null;
        } // GetByMoves

        public Eco GetByMoves(List<ChessLib.Game.MoveNotation> moves)
        {
            Eco foundEco = null;
            if (moves != null) {
                StringBuilder sb = new StringBuilder();
                foreach (var m in moves) {
                    sb.Append($"{m.ShortAlgebraic} ");
                    var tEco = GetByMoves(sb.ToString());
                    if (tEco != null)
                        foundEco = tEco;
                    else
                        break;
                }
            }
            return foundEco;
        } // GetByMoves

        public async Task<bool> Load(Stream stream)
        {
            StringBuilder sb = new StringBuilder();
            var pgnGames = await ChessLib.PGN.LoadFromStream(stream);
            foreach (var pg in pgnGames) {
                Eco eco = new Eco()
                {
                    Name = pg.White,
                    Variation = pg.Black,
                    Code = pg.ECO
                };

                sb.Clear();
                foreach (var m in pg.Moves) {
                    sb.Append($"{m.Notation} ");
                }
                eco.Moves = sb.ToString().Trim();
                m_EcoDatabaseByMoves[eco.Moves] = eco;
            }
            return true;
        } // Load
    }
}