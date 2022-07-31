using System;

namespace ChessLib.Exceptions
{
    public class InvalidMoveException : Exception
    {
        public InvalidMoveException(string move, string reason, string fen)
        {
            Move = move;
            Reason = reason;
            FEN = fen;
        }

        public string Move { get; private set; }
        public string Reason { get; private set; }
        public string FEN { get; private set; }

        public override string Message {
            get {
                return $"Invalid move {Move} ({Reason}), FEN '{FEN}'";
            }
        }
    }
}