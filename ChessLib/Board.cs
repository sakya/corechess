using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChessLib
{
    public class Board : IdObject
    {
        #region classes
        public class Square
        {
            public Square(int rank, char file)
            {
                Rank = rank;
                File = file;
            }

            public int Rank { get; set; }
            public char File { get; set; }
            public string Notation
            {
                get {
                    return $"{File}{Rank}";
                }
            }

            public Piece Piece { get; set; }
            public Game.Colors Color
            {
                get {
                    int delta = 0;
                    if (Rank % 2 == 1)
                        delta = 1;
                    return (Rank * 8 + File - 65 + delta) % 2 == 0 ? Game.Colors.White : Game.Colors.Black;
                }
            }
        } // Square
        #endregion

        private readonly List<char> m_Files = new List<char>() { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H' };

        public Board()
        {
            Squares = new List<Square>();
        }

        [JsonIgnore]
        public List<char> Files
        {
            get { return m_Files; }
        }

        public List<Square> Squares { get; set; }

        /// <summary>
        /// The initial king square (white, black)
        /// </summary>
        /// <value></value>
        public char[] InitialKingSquare { get; set; } = new char[2];

        /// <summary>
        /// Get a square from its notation
        /// </summary>
        /// <param name="notation">The notation (eg. e2)</param>
        /// <returns></returns>
        public Square GetSquare(string notation)
        {
            if (string.IsNullOrEmpty(notation))
                throw new ArgumentNullException(nameof(notation));
            if (notation.Length != 2)
                throw new ArgumentException("Must be 2 characters long", nameof(notation));

            char file = char.ToUpper(notation[0]);
            int rank = int.Parse(notation[1].ToString()) - 1;

            return Squares[rank * 8 + m_Files.IndexOf(file)];
        } // GetSquare

        /// <summary>
        /// Get the <see cref="Square"/> where the given <see cref="Piece"/> is
        /// </summary>
        /// <param name="piece">The <see cref="Piece"/></param>
        /// <returns>A <see cref="Square"/> or null</returns>
        public Square GetSquare(Piece piece)
        {
            foreach (var f in Squares) {
                if (f.Piece?.Id == piece.Id)
                    return f;
            }
            return null;
        } // GetSquare

        /// <summary>
        /// Init the board with no pieces
        /// </summary>
        public void InitEmpty()
        {
            for (int i = 0; i < 64; i++) {
                Squares.Add(new Square(i / 8 + 1, m_Files[i % 8]));
            }
        } // InitEmpty

        /// <summary>
        /// Init the board with initial position
        /// </summary>
        public void Init(bool chess960=false)
        {
            Squares.Clear();
            InitEmpty();

            List<Piece.Pieces> order = null;
            if (chess960) {
                order = CreateRandomPosition();
            } else {
                order = new List<Piece.Pieces>()
                {
                    Piece.Pieces.Rook,
                    Piece.Pieces.Knight,
                    Piece.Pieces.Bishop,
                    Piece.Pieces.Queen,
                    Piece.Pieces.King,
                    Piece.Pieces.Bishop,
                    Piece.Pieces.Knight,
                    Piece.Pieces.Rook
                };
            }

            // White
            GetSquare("A2").Piece = new Piece(Game.Colors.White, Piece.Pieces.Pawn);
            GetSquare("B2").Piece = new Piece(Game.Colors.White, Piece.Pieces.Pawn);
            GetSquare("C2").Piece = new Piece(Game.Colors.White, Piece.Pieces.Pawn);
            GetSquare("D2").Piece = new Piece(Game.Colors.White, Piece.Pieces.Pawn);
            GetSquare("E2").Piece = new Piece(Game.Colors.White, Piece.Pieces.Pawn);
            GetSquare("F2").Piece = new Piece(Game.Colors.White, Piece.Pieces.Pawn);
            GetSquare("G2").Piece = new Piece(Game.Colors.White, Piece.Pieces.Pawn);
            GetSquare("H2").Piece = new Piece(Game.Colors.White, Piece.Pieces.Pawn);
            GetSquare("A1").Piece = new Piece(Game.Colors.White, order[0]);
            GetSquare("B1").Piece = new Piece(Game.Colors.White, order[1]);
            GetSquare("C1").Piece = new Piece(Game.Colors.White, order[2]);
            GetSquare("D1").Piece = new Piece(Game.Colors.White, order[3]);
            GetSquare("E1").Piece = new Piece(Game.Colors.White, order[4]);
            GetSquare("F1").Piece = new Piece(Game.Colors.White, order[5]);
            GetSquare("G1").Piece = new Piece(Game.Colors.White, order[6]);
            GetSquare("H1").Piece = new Piece(Game.Colors.White, order[7]);

            // Black
            GetSquare("A7").Piece = new Piece(Game.Colors.Black, Piece.Pieces.Pawn);
            GetSquare("B7").Piece = new Piece(Game.Colors.Black, Piece.Pieces.Pawn);
            GetSquare("C7").Piece = new Piece(Game.Colors.Black, Piece.Pieces.Pawn);
            GetSquare("D7").Piece = new Piece(Game.Colors.Black, Piece.Pieces.Pawn);
            GetSquare("E7").Piece = new Piece(Game.Colors.Black, Piece.Pieces.Pawn);
            GetSquare("F7").Piece = new Piece(Game.Colors.Black, Piece.Pieces.Pawn);
            GetSquare("G7").Piece = new Piece(Game.Colors.Black, Piece.Pieces.Pawn);
            GetSquare("H7").Piece = new Piece(Game.Colors.Black, Piece.Pieces.Pawn);
            GetSquare("A8").Piece = new Piece(Game.Colors.Black, order[0]);
            GetSquare("B8").Piece = new Piece(Game.Colors.Black, order[1]);
            GetSquare("C8").Piece = new Piece(Game.Colors.Black, order[2]);
            GetSquare("D8").Piece = new Piece(Game.Colors.Black, order[3]);
            GetSquare("E8").Piece = new Piece(Game.Colors.Black, order[4]);
            GetSquare("F8").Piece = new Piece(Game.Colors.Black, order[5]);
            GetSquare("G8").Piece = new Piece(Game.Colors.Black, order[6]);
            GetSquare("H8").Piece = new Piece(Game.Colors.Black, order[7]);

            // Set initial square color
            foreach (var f in Squares) {
                if (f.Piece != null)
                    f.Piece.InitialSquareColor = f.Color;
            }

            InitialKingSquare[0] = GetKingSquare(Game.Colors.White).File;
            InitialKingSquare[1] = GetKingSquare(Game.Colors.Black).File;
        } // Init

        /// <summary>
        /// Initialize the board from a FEN string
        /// </summary>
        /// <param name="FenString">The FEN string</param>
        /// <param name="initialPosition">True if this is the board initial position (eg: Chess960)</param>
        public void InitFromFenString(string fenString)
        {
            fenString = fenString.Trim();
            Squares.Clear();
            for (int i = 0; i < 64; i++) {
                Squares.Add(new Square(i / 8 + 1, m_Files[i % 8]));
            }

            int endIdx = fenString.IndexOf(" ");
            int rank = 8;
            int file = 0;
            for (int i = 0; i < endIdx; i++) {
                char c = fenString[i];
                Piece piece = null;

                if (char.IsDigit(c)) {
                    // Empty spaces
                    file += int.Parse(c.ToString());
                } else if (c == '/') {
                    // End rank
                    rank--;
                    file = 0;
                } else if (char.IsLetter(c)) {
                    Game.Colors color = char.IsUpper(c) ? Game.Colors.White : Game.Colors.Black;
                    piece = new Piece(color, Piece.GetTypeFromAcronym(c));
                    GetSquare($"{Files[file]}{rank}").Piece = piece;
                    file++;
                }
            }
        } // InitFromFenString

        /// <summary>
        /// Get the board FEN string
        /// </summary>
        /// <returns></returns>
        public string GetFenString()
        {
            StringBuilder sb = new StringBuilder();
            for (int rank = 8; rank > 0; rank--) {
                int emptyCount = 0;
                foreach (char file in Files) {
                    var square = GetSquare($"{file}{rank}");
                    if (square.Piece != null) {
                        if (emptyCount > 0)
                            sb.Append(emptyCount);
                        if (square.Piece.Color == Game.Colors.Black)
                            sb.Append(char.ToLower(square.Piece.Acronym));
                        else
                            sb.Append(char.ToUpper(square.Piece.Acronym));
                        emptyCount = 0;
                    } else {
                        emptyCount++;
                    }
                }
                if (emptyCount > 0)
                    sb.Append(emptyCount);
                if (rank > 1)
                    sb.Append("/");
            }

            return sb.ToString();
        } // GetFenString

        /// <summary>
        /// Get an ASCII representation of the board
        /// </summary>
        /// <returns></returns>
        public string GetAscii()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Id: {Id}");
            for (int row = 8; row > 0; row--) {
                sb.Append($"{row} ");
                foreach (var file in Files) {
                    var square = GetSquare($"{file}{row}");
                    char c = square.Piece?.Acronym ?? ' ';
                    if (square.Piece?.Color == Game.Colors.Black)
                        c = char.ToLower(c);
                    sb.Append($" {c}");
                }
                sb.Append('\n');
            }
            sb.AppendLine("   A B C D E F G H");
            return sb.ToString();
        } // GetAscii

        public char? GetPreviousFile(char file)
        {
            int idx = Files.IndexOf(file);
            if (idx > 0)
                return Files[idx - 1];
            return null;
        } // GetPreviousFile

        public char? GetNextFile(char file)
        {
            int idx = Files.IndexOf(file);
            if (idx >= 0 && idx < Files.Count - 1)
                return Files[idx + 1];
            return null;
        } // GetNextFile

        /// <summary>
        /// Get the file with the King of the given color
        /// </summary>
        /// <param name="color">The king's color</param>
        /// <returns>The <see cref="Board.Square"/> with the King</returns>
        public Square GetKingSquare(Game.Colors color)
        {
            foreach (var f in Squares) {
                if (f.Piece != null && f.Piece.Type == Piece.Pieces.King && f.Piece.Color == color)
                    return f;
            }
            return null;
        } // GetKingSquare

        /// <summary>
        /// Get all the pieces of the given color
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public List<Piece> GetPieces(Game.Colors color)
        {
            List<Piece> res = new List<Piece>();
            foreach (Square f in Squares) {
                if (f.Piece?.Color == color)
                    res.Add(f.Piece);
            }
            return res;
        } // GetPieces

        #region private operations
        /// <summary>
        /// Create random a position for the first rank
        /// </summary>
        /// <returns></returns>
        private List<Piece.Pieces> CreateRandomPosition()
        {
            var order = new Piece.Pieces[8];
            List<int> freePos = new List<int>() {0, 1, 2, 3, 4, 5, 6, 7};
            Random rnd = new Random();

            // King
            int kIdx = rnd.Next(1, 7);
            order[kIdx] = Piece.Pieces.King;
            freePos.Remove(kIdx);

            // Rooks
            int idx = rnd.Next(0, kIdx);
            order[idx] = Piece.Pieces.Rook;
            freePos.Remove(idx);
            idx = rnd.Next(kIdx + 1, 7);
            order[idx] = Piece.Pieces.Rook;
            freePos.Remove(idx);

            // Bishops
            var avail = freePos.Where(i => i % 2 == 0).ToList();
            idx = avail[rnd.Next(0, avail.Count)];
            order[idx] = Piece.Pieces.Bishop;
            freePos.Remove(idx);
            avail = freePos.Where(i => i % 2 != 0).ToList();
            idx = avail[rnd.Next(0, avail.Count)];
            order[idx] = Piece.Pieces.Bishop;
            freePos.Remove(idx);

            // Queen
            idx = freePos[rnd.Next(0, freePos.Count)];
            order[idx] = Piece.Pieces.Queen;
            freePos.Remove(idx);

            // Knights
            order[freePos[0]] = Piece.Pieces.Knight;
            order[freePos[1]] = Piece.Pieces.Knight;

            return order.ToList();
        } // CreateRandomPosition
        #endregion
    }
}
