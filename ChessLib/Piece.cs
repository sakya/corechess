using System;

namespace ChessLib
{
    public class Piece : IdObject
    {
        public enum Pieces
        {
            Pawn,
            Knight,
            Bishop,
            Rook,
            Queen,
            King
        }

        public Piece(Game.Colors color, Pieces type)
            : base()
        {
            Color = color;
            Type = type;
        }

        public Game.Colors Color { get; set; }
        public Game.Colors InitialSquareColor { get; set; }
        public Pieces Type { get; set; }
        public bool Moved { get; set; }
        public char Acronym
        {
            get {
                switch (Type)
                {
                    case Pieces.Pawn:
                        return 'P';
                    case Pieces.Knight:
                        return 'N';
                    case Pieces.Bishop:
                        return 'B';
                    case Pieces.Rook:
                        return 'R';
                    case Pieces.Queen:
                        return 'Q';
                    case Pieces.King:
                        return 'K';
                    default:
                        return ' ';
                }
            }
        }

        public int Value
        {
            get {
                switch (Type) {
                    case Pieces.Pawn:
                        return 1;
                    case Pieces.Knight:
                        return 3;
                    case Pieces.Bishop:
                        return 3;
                    case Pieces.Rook:
                        return 5;
                    case Pieces.Queen:
                        return 9;
                    case Pieces.King:
                        return int.MaxValue;
                    default:
                        return 0;
                }
            }
        }

        public static Pieces GetTypeFromAcronym(char acronym)
        {
            switch (acronym) {
                case 'P':
                case 'p':
                    return Pieces.Pawn;
                case 'N':
                case 'n':
                    return Pieces.Knight;
                case 'B':
                case 'b':
                    return Pieces.Bishop;
                case 'R':
                case 'r':
                    return Pieces.Rook;
                case 'Q':
                case 'q':
                    return Pieces.Queen;
                case 'K':
                case 'k':
                    return Pieces.King;
                default:
                    throw new ArgumentException("Invalid acronym", nameof(acronym));
            }
        }
    }
}
