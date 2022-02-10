using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChessLib.EBoards
{
    public abstract class EBoard : IdObject, IDisposable
    {
        public abstract class EBoardSettings
        {
            public string Name { get; set; }
        }

        public EBoard(EBoardSettings settings)
            : base()
        {
            Settings = settings;
        }

        public EBoardSettings Settings { get; set; }

        public abstract Task<bool> Init();

        /// <summary>
        /// Get the board status (FEN string)
        /// </summary>
        /// <returns></returns>
        public abstract Task<string> GetBoard();

        public abstract void Dispose();
    }
}