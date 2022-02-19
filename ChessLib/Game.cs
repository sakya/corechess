using ChessLib.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ChessLib.Extensions;

namespace ChessLib
{
    public class Game : IdObject, IDisposable
    {
        #region classes
        public class GameSettings
        {
            public GameSettings()
            {
                Event = "Casual Game";
                Players = new List<Player>();
                Date = DateTime.UtcNow.Date;
                DrawForRepetition = 5;
            }

            public string Event { get; set; }
            public List<Player> Players { get; set; }
            public Player WhitePlayer
            {
                get {
                    return Players.Where(p => p.Color == Colors.White).FirstOrDefault();
                }
            }
            public string WhitePlayerName {
                get {
                    return Players.Where(p => p.Color == Colors.White).FirstOrDefault()?.Name;
                }
            }
            public string WhitePlayerDisplayName
            {
                get
                {
                    return Players.Where(p => p.Color == Colors.White).FirstOrDefault()?.DisplayName;
                }
            }
            public Player BlackPlayer
            {
                get {
                    return Players.Where(p => p.Color == Colors.Black).FirstOrDefault();
                }
            }
            public string BlackPlayerName {
                get {
                    return Players.Where(p => p.Color == Colors.Black).FirstOrDefault()?.Name;
                }
            }
            public string BlackPlayerDisplayName
            {
                get
                {
                    return Players.Where(p => p.Color == Colors.Black).FirstOrDefault()?.DisplayName;
                }
            }
            public bool TrainingMode { get; set; }
            public DateTime? Date { get; set; }
            public bool IsChess960 { get; set; }
            /// <summary>
            /// Get the human player color. If there's no human player or two human player null is returned
            /// </summary>
            /// <value></value>
            public Colors? HumanPlayerColor {
                get {
                    List<Player> hp = Players.Where(p => p is HumanPlayer).ToList();
                    if (hp.Count == 1)
                        return hp[0].Color;
                    return null;
                }
            }
            public string InitialFenPosition { get; set; }
            public TimeSpan? MaximumTime { get; set; }
            public TimeSpan? MaxEngineThinkingTime { get; set; }
            public TimeSpan? TimeIncrement { get; set; }
            public int? EngineDepth { get; set; }
            public int DrawForRepetition { get; set; }
        } // GameSettings

        public class Move
        {
            public Move(Piece piece, Board.Square from, Board.Square to)
            {
                Piece = piece;
                From = from;
                To = to;
            }
            public Piece Piece { get; set; }
            public Piece CapturedPiece { get; set; }
            public Board.Square From { get; set; }
            public Board.Square To { get; set; }
            public string CoordinateNotation { get; set; }
            public DateTime Timestamp { get; set; }
        } // Move

        public class MoveNotation
        {
            /// <summary>
            /// Move index (a white and a black move has the same index)
            /// </summary>
            public int Index { get; set; }
            public Colors? Color { get; set; }

            /// <summary>
            /// Short algebraic notation (eg. e4)
            /// </summary>
            public string ShortAlgebraic { get; set; }
            /// <summary>
            /// Long algebraic notation (eg. Nb1c3)
            /// </summary>
            public string LongAlgebraic { get; set; }
            /// <summary>
            /// Coordinate notation (eg. e2e4)
            /// </summary>
            public string Coordinate { get; set; }
            public string Comment { get; set; }

            /// <summary>
            /// ?? (Blunder)
            /// ? (Mistake)
            /// ?! (Dubious move)
            /// !? (Interesting move)
            /// ! (Good move)
            /// !! (Brilliant move)
            /// </summary>
            /// <value></value>
            public string Annotation { get; set; }
            /// <summary>
            /// The FEN position after this move
            /// </summary>
            /// <value></value>
            public string Fen { get; set; }
        } // MoveNotation

        class MoveResult
        {
            public MoveResult()
            {
                Moves = new List<Move>();
                Promoted = false;
                Ambiguous = new List<Board.Square>();
            }

            public string Move { get; set; }
            public List<Move> Moves { get; set; }
            public bool Promoted { get; set; }
            public List<Board.Square> Ambiguous { get; set; }
        } // MoveResult

        #endregion
        public enum Statuses
        {
            None,
            InProgress,
            Paused,
            Ended,
            Stopped
        }

        public enum Colors
        {
            White,
            Black
        }

        public enum Results
        {
            Checkmate,
            Resignation,
            Timeout,

            Stalemate,
            Draw,
            Aborted
        }

        public const string StandardInitialFenPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        private static JsonSerializerSettings m_SerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects, NullValueHandling = NullValueHandling.Ignore };
        private System.Timers.Timer m_WhiteTimer = new System.Timers.Timer(100);
        private System.Timers.Timer m_BlackTimer = new System.Timers.Timer(100);
        private Random m_Rnd = new Random();

        Task<Engines.EngineBase.BestMove> m_PonderTask = null;
        EnginePlayer m_PonderPlayer = null;
        string m_PonderingMove = string.Empty;

        #region events
        public event EventHandler WhiteTimer;
        public event EventHandler BlackTimer;

        public class PromotionArgs : EventArgs
        {
            public PromotionArgs(Move move)
            {
                Move = move;
            }

            public Move Move { get; set; }
        }
        public delegate Task<Piece.Pieces> PromotionHandler(object sender, PromotionArgs e);
        /// <summary>
        /// Raised when the player promotes a pawn. Must return the piece to promote to
        /// </summary>
        public event PromotionHandler PlayerPromotion;

        public delegate bool PromotedHandler(object sender, PromotionArgs e);
        /// <summary>
        /// Raised when a pawn is promoted
        /// </summary>
        public event PromotedHandler Promoted;

        public delegate Task<bool> CastlingConfirmHandler(object sender, EventArgs e);
        /// <summary>
        /// Raised when the player must confirm castling (Chess960)
        /// </summary>
        public event CastlingConfirmHandler CastlingConfirm;

        public delegate void EngineErrorHandler(object sender, string error);
        public event EngineErrorHandler EngineError;
        #endregion

        public Game()
            : base()
        {
            Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Winner = null;
            Board = new Board();
            Moves = new List<MoveNotation>();
            CapturedPieces = new List<Piece>();
            ToMove = Colors.White;
            KingCastling = new List<Colors>();
            QueenCastling = new List<Colors>();
            FullmoveNumber = 0;

            m_WhiteTimer.Elapsed += OnWhiteTimerElapsed;
            m_BlackTimer.Elapsed += OnBlackTimerElapsed;
        }

        public void Dispose()
        {
            m_WhiteTimer.Stop();
            m_WhiteTimer.Dispose();

            m_BlackTimer.Stop();
            m_BlackTimer.Dispose();
        } // Dispose

        #region public properties
        public Version Version { get; set; }

        /// <summary>
        /// The game initial position (FEN)
        /// </summary>
        public string InitialFenPosition { get; set; }

        /// <summary>
        /// The king castling move notation
        /// </summary>
        public string WhiteKingCastlingMove { get; set; }
        public string BlackKingCastlingMove { get; set; }

        /// <summary>
        /// The queen castling move notation
        /// </summary>
        public string WhiteQueenCastlingMove { get; set; }
        public string BlackQueenCastlingMove { get; set; }

        public string GameType {
            get {
                string res = string.Empty;
                if (Settings.IsChess960)
                    res = "Chess960 - ";

                if (Settings.MaximumTime.HasValue) {
                    if (Settings.MaximumTime.Value <= TimeSpan.FromMinutes(3))
                        res = $"{res}Lightening";
                    else if (Settings.MaximumTime.Value < TimeSpan.FromMinutes(15))
                        res = $"{res}Blitz";
                    else
                        res = $"{res}Standard";
                } else {
                    res = $"{res}Untimed";
                }

                if (Settings.TrainingMode)
                    res = $"{res} (training)";
                return res;
            }
        }

        /// <summary>
        /// The name of the file this game has been loaded from or saved to
        /// </summary>
        /// <value></value>
        [JsonIgnore]
        public string FileName { get; set; }
        public GameSettings Settings { get; set; }
        public DateTime StartedTime { get; set; }
        public Statuses Status { get; set; }
        public string ECO { get; set; }
        public bool Ended {
            get {
                return Status == Statuses.Ended || Status == Statuses.Stopped;
            }
        }
        public Results? Result { get; set; }
        public Colors? Winner { get; set; }
        public Board Board { get; set; }
        public Colors ToMove { get; set; }
        public Player ToMovePlayer
        {
            get {
                return GetPlayer(ToMove);
            }
        }
        public List<Colors> KingCastling { get; set; }
        public List<Colors> QueenCastling { get; set; }
        public string EnPassant { get; set; }
        public Dictionary<string, int> Positions = new Dictionary<string, int>();

        /// <summary>
        /// The list of moves.
        /// </summary>
        public List<MoveNotation> Moves { get; set; }

        public List<Piece> CapturedPieces { get; set; }
        public int HalfmoveClock { get; set; }
        public int FullmoveNumber { get; set; }

        public int WhiteTimeMilliSecs { get; set; }
        public int? WhiteTimeLeftMilliSecs { get; set; }
        public int WhiteIncrementMillisecs { get; set; }
        public int BlackTimeMilliSecs { get; set; }
        public int? BlackTimeLeftMilliSecs { get; set; }
        public int BlackIncrementMillisecs { get; set; }
        public List<Engines.EngineBase.AnalyzeResult> AnalyzeResults { get; set; }

        public int? LastWhiteTimeLeftMilliSecs { get; set; }
        public int? LastBlackTimeLeftMilliSecs { get; set; }
        #endregion

        /// <summary>
        /// Initializes the game
        /// </summary>
        /// <param name="fenString">Optional FEN string</param>
        public void Init(GameSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // Check players
            if (settings.Players == null || settings.Players.Count != 2 || settings.Players.Where(p => p.Color == Colors.White).Count() != 1)
                throw new Exception("Invalid players");

            if (string.IsNullOrEmpty(settings.InitialFenPosition)) {
                Board.Init(settings.IsChess960);
                InitialFenPosition = GetFenString();
                KingCastling = new List<Colors>() { Colors.White, Colors.Black };
                QueenCastling = new List<Colors>() { Colors.White, Colors.Black };
            } else {
                string fenString = settings.InitialFenPosition;
                while (fenString.IndexOf("  ") >= 0)
                    fenString = fenString.Replace("  ", " ");

                Board.InitFromFenString(fenString);
                InitialFenPosition = fenString;
                string[] parts = fenString.Split(' ');
                ToMove = parts[1] == "w" ? Colors.White : Colors.Black;

                if (parts.Length > 2) {
                    KingCastling.Clear();
                    QueenCastling.Clear();
                    if (parts[2].Contains("K"))
                        KingCastling.Add(Colors.White);
                    if (parts[2].Contains("Q"))
                        QueenCastling.Add(Colors.White);
                    if (parts[2].Contains("k"))
                        KingCastling.Add(Colors.Black);
                    if (parts[2].Contains("q"))
                        QueenCastling.Add(Colors.Black);
                }

                if (parts.Length > 3 && parts[3] != "-")
                    EnPassant = parts[3];

                if (parts.Length > 4)
                    HalfmoveClock = int.Parse(parts[4]);
                if (parts.Length > 5)
                    FullmoveNumber = int.Parse(parts[5]);
            }

            var kingSquare = Board.GetKingSquare(Colors.White);
            if (KingCastling.Contains(Colors.White))
                WhiteKingCastlingMove = $"{kingSquare.Notation.ToLower()}g1";
            if (QueenCastling.Contains(Colors.White))
                WhiteQueenCastlingMove = $"{kingSquare.Notation.ToLower()}c1";
            kingSquare = Board.GetKingSquare(Colors.Black);
            if (KingCastling.Contains(Colors.White))
                BlackKingCastlingMove = $"{kingSquare.Notation.ToLower()}g8";
            if (QueenCastling.Contains(Colors.White))
                BlackQueenCastlingMove = $"{kingSquare.Notation.ToLower()}c8";

            if (settings.MaximumTime.HasValue) {
                WhiteTimeLeftMilliSecs = (int)settings.MaximumTime.Value.TotalMilliseconds;
                BlackTimeLeftMilliSecs = (int)settings.MaximumTime.Value.TotalMilliseconds;

                LastWhiteTimeLeftMilliSecs = WhiteTimeLeftMilliSecs;
                LastBlackTimeLeftMilliSecs = BlackTimeLeftMilliSecs;
            }

            if (settings.TimeIncrement.HasValue) {
                WhiteIncrementMillisecs = (int)settings.TimeIncrement.Value.TotalMilliseconds;
                BlackIncrementMillisecs = (int)settings.TimeIncrement.Value.TotalMilliseconds;
            }
            Settings = settings;
        } // Init

        public async Task<bool> Start()
        {
            if (!Ended) {
                await InitEnginePlayers();

                StartedTime = DateTime.UtcNow;
                BlackTimer?.Invoke(this, new EventArgs());
                WhiteTimer?.Invoke(this, new EventArgs());
                if (FullmoveNumber > 0) {
                    if (ToMove == Colors.White) {
                        m_WhiteTimer.Start();
                        m_BlackTimer.Stop();
                    } else {
                        m_WhiteTimer.Stop();
                        m_BlackTimer.Start();
                    }
                }

                Status = Statuses.InProgress;
            }
            return true;
        } // Start

        public async Task<bool> Stop()
        {
            m_WhiteTimer.Stop();
            m_BlackTimer.Stop();

            Status = Statuses.Stopped;

            await StopPondering();
            foreach (var p in Settings.Players) {
                if (p is EnginePlayer) {
                    var ep = p as EnginePlayer;
                    if (ep.Engine != null)
                        await ep.Engine.Stop();
                }
            }
            return true;
        } // Stop

        public bool Pause()
        {
            if (!Ended) {
                Status = Statuses.Paused;
                if (ToMovePlayer is HumanPlayer) {
                    if (ToMovePlayer.Color == Colors.White)
                        m_WhiteTimer.Stop();
                    else
                        m_BlackTimer.Stop();
                }
                return true;
            }
            return false;
        } // Pause

        public bool Resume()
        {
            if (Status == Statuses.Paused) {
                Status = Statuses.InProgress;
                if (ToMovePlayer is HumanPlayer) {
                    if (FullmoveNumber > 0) {
                        if (ToMovePlayer.Color == Colors.White)
                            m_WhiteTimer.Start();
                        else
                            m_BlackTimer.Start();
                    }
                }
                return true;
            }
            return false;
        } // Resume

        public async Task<List<Move>> DoHumanPlayerMove(string move)
        {
            if (!(ToMovePlayer is HumanPlayer))
                throw new Exception("Not a human player");
            var res = await DoMove(move);
            if (ToMovePlayer is EnginePlayer)
                await (ToMovePlayer as EnginePlayer).Engine.ForceMove(res[0].CoordinateNotation.ToLower());
            return res;
        } // DoHumanPlayerMove

        public async Task<List<Move>> DoEnginePlayerMove(Action<Engines.EngineBase, string> outputCallback = null)
        {
            if (!(ToMovePlayer is EnginePlayer))
                throw new Exception("Not an engine player");

            List<Move> res = null;
            Engines.EngineBase.BestMove engineMove = null;
            var enginePlayer = ToMovePlayer as EnginePlayer;

            List<string> moves = new List<string>();
            foreach (var m in Moves)
                moves.Add(m.Coordinate);

            if (m_PonderTask != null) {
                // Pondering
                if (Moves.Last().Coordinate == m_PonderingMove) {
                    await enginePlayer.Engine.Ponderhit();
                    engineMove = await m_PonderTask;
                } else {
                    await enginePlayer.Engine.StopCommand();
                    await m_PonderTask;
                }
                m_PonderTask.Dispose();
                m_PonderTask = null;
                m_PonderPlayer = null;
            } else {
                // Search opening book
                if (enginePlayer.OpeningBook != null && !enginePlayer.Engine.IsOwnBookEnabled() && !Settings.IsChess960) {
                    List<Books.IBookEntry> bookMoves = null;
                    if (enginePlayer.OpeningBook.SupportGetFromFen())
                        bookMoves = enginePlayer.OpeningBook.GetMovesFromFen(GetFenString());
                    else if (enginePlayer.OpeningBook.SupportGetFromMoves())
                        bookMoves = enginePlayer.OpeningBook.GetMovesFromMoves(moves);

                    if (bookMoves != null && bookMoves.Count > 0) {
                        string bookMove = string.Empty;
                        if (bookMoves.Count == 1) {
                            bookMove = bookMoves[0].GetMove();
                        } else {
                            int totPriority = bookMoves.Sum(bm => bm.GetPriotity());
                            List<int> probs = new List<int>();
                            foreach (var bm in bookMoves) {
                                probs.Add((int)Math.Round((double)bm.GetPriotity() / (double)totPriority * 100.0, 0));
                            }
                            bookMove = bookMoves[m_Rnd.GetAlias(probs)].GetMove();
                        }

                        res = await DoMove(bookMove);
                        await enginePlayer.Engine.ForceMove(bookMove);
                    }
                }
            }

            if (res == null) {
                if (engineMove == null) {
                    await enginePlayer.Engine.SetPosition(InitialFenPosition, moves);
                    CancellationTokenSource cts = new CancellationTokenSource();
                    engineMove = await enginePlayer.Engine.GetBestMove(WhiteTimeLeftMilliSecs ?? 0, WhiteIncrementMillisecs, BlackTimeLeftMilliSecs ?? 0,
                        BlackIncrementMillisecs, Settings.EngineDepth,
                        Settings.MaxEngineThinkingTime > TimeSpan.Zero ? Settings.MaxEngineThinkingTime : null,
                        cts.Token, null,
                        (output) => outputCallback(enginePlayer.Engine, output));
                    cts.Dispose();

                    if (enginePlayer.Engine is Engines.Cecp) {
                        // CECP returns moves in short algebraic, convert it to coordinate
                        char promotion;
                        string saMove = GetCoordinateNotation(enginePlayer.Color, engineMove.Move, out promotion, out _);
                        if (promotion != '\0')
                            engineMove.Move = $"{saMove}{promotion}";
                        else
                            engineMove.Move = saMove;
                    }
                }

                // Ended is set to true when Stop() is called (eg: new game while the engine is thinking)
                if (Ended)
                    return null;
                res = await DoMove(engineMove.Move);
            }

            // Start pondering
            if (engineMove != null && !string.IsNullOrEmpty(engineMove.Ponder) && enginePlayer.Engine is Engines.Uci && enginePlayer.Engine.IsPonderingEnabled())
                m_PonderTask = PonderMove(enginePlayer, engineMove.Ponder, outputCallback);

            return res;
        } // DoEnginePlayerMove

        public async Task<List<Move>> DoMove(string move, bool checkEndGame = true, bool skipAvailableSquaresCheck = false)
        {
            // Check move
            if (string.IsNullOrEmpty(move))
                throw new InvalidMoveException(move, "empty", GetFenString());

            int moveIndex = 0;
            if (Moves.Count == 0)
                moveIndex = 1;
            else {
                if (Moves.Count % 2 == 0)
                    moveIndex = Moves.Last().Index + 1;
                else
                    moveIndex = Moves.Last().Index;
            }

            bool promoted = false;
            var res = new List<Move>();
            move = move.Trim().ToLower();

            // Remove "="
            int idx = move.IndexOf("=");
            if (idx >= 0)
                move = move.Remove(idx, 1);

            List<Board.Square> ambiguous = null;
            // Check castling
            if (move == WhiteKingCastlingMove || move == BlackKingCastlingMove || move == WhiteQueenCastlingMove || move == BlackQueenCastlingMove) {
                var tempSquare = Board.GetSquare(move.Substring(0, 2));
                if (tempSquare.Piece?.Type == Piece.Pieces.King) {
                    bool confirmed = true;

                    if (Settings.IsChess960 && ToMovePlayer is HumanPlayer){
                        if (CastlingConfirm != null)
                            confirmed = await CastlingConfirm.Invoke(this, new EventArgs());
                    }

                    if ((confirmed && move == WhiteKingCastlingMove) || move == BlackKingCastlingMove)
                        move = "0-0";
                    else if ((confirmed && move == WhiteQueenCastlingMove) || move == BlackQueenCastlingMove)
                        move = "0-0-0";
                }
            }

            if (move == "0-0") {
                // King castling
                var moveRes = DoMoveKingCatling(move);
                move = moveRes.Move;
                res.AddRange(moveRes.Moves);
            } else if (move == "0-0-0") {
                // Queen castling
                var moveRes = DoMoveQueenCatling(move);
                move = moveRes.Move;
                res.AddRange(moveRes.Moves);
            } else {
                var moveRes = await DoMoveNormal(move, skipAvailableSquaresCheck);
                move = moveRes.Move;
                promoted = moveRes.Promoted;
                ambiguous = moveRes.Ambiguous;
                res.AddRange(moveRes.Moves);
            }

            // Count the position for the draw
            string fen = Board.GetFenString();
            int count;
            if (Positions.TryGetValue(fen, out count))
                Positions[fen] = count + 1;
            else
                Positions[fen] = 1;

            if (checkEndGame) {
                // Check game ended
                var nextTurn = ToMove == Colors.White ? Colors.Black : Colors.White;
                if (await IsCheckmate(nextTurn)) {
                    Status = Statuses.Ended;
                    Result = Results.Checkmate;
                    Winner = ToMove;
                } else if (await IsStalemate(nextTurn)) {
                    Status = Statuses.Ended;
                    Result = Results.Stalemate;
                    Winner = null;
                } else if (IsDraw()) {
                    Status = Statuses.Ended;
                    Result = Results.Draw;
                    Winner = null;
                }

                if (Ended)
                    await StopPondering();
            }

            var timeStamp = DateTime.UtcNow;
            if (ToMove == Colors.White) {
                ToMove = Colors.Black;
                if (Status == Statuses.InProgress || Status == Statuses.Paused) {
                    if (Status == Statuses.InProgress)
                        m_BlackTimer.Start();
                    WhiteTimeLeftMilliSecs += WhiteIncrementMillisecs;
                    WhiteTimer?.Invoke(this, new EventArgs());
                }
                m_WhiteTimer.Stop();
            } else {
                ToMove = Colors.White;
                if (Status == Statuses.InProgress || Status == Statuses.Paused) {
                    if (Status == Statuses.InProgress)
                        m_WhiteTimer.Start();
                    BlackTimeLeftMilliSecs += BlackIncrementMillisecs;
                    BlackTimer?.Invoke(this, new EventArgs());
                }
                m_BlackTimer.Stop();
            }
            foreach (var m in res)
                m.Timestamp = timeStamp;

            MoveNotation moveNotation = new MoveNotation()
            {
                Coordinate = move,
                Index = moveIndex,
                Color = res[0].Piece.Color
            };

            // Algebraic notation
            SetAlgebraicNotation(moveNotation, move, res, promoted, ambiguous);

            moveNotation.Fen = GetFenString();

            LastWhiteTimeLeftMilliSecs = WhiteTimeLeftMilliSecs;
            LastBlackTimeLeftMilliSecs = BlackTimeLeftMilliSecs;
            Moves.Add(moveNotation);

            FullmoveNumber++;
            if (res.Count == 1 && (res[0].CapturedPiece != null || res[0].Piece.Type == Piece.Pieces.Pawn))
                HalfmoveClock = 0;
            else
                HalfmoveClock++;

            return res;
        } // DoMove

        public async Task<bool> Resign()
        {
            if (!Ended) {
                m_WhiteTimer.Stop();
                m_BlackTimer.Stop();
                Status = Statuses.Ended;
                Result = Results.Resignation;
                Winner = ToMovePlayer.Color == Colors.White ? Colors.Black : Colors.White;

                var move = Moves.Count > 0 ? Moves.Last() : null;
                if (move != null) {
                    if (Winner == Colors.White) {
                        move.ShortAlgebraic = $"{move.ShortAlgebraic} 1-0";
                        move.LongAlgebraic = $"{move.LongAlgebraic} 1-0";
                    } else {
                        move.ShortAlgebraic = $"{move.ShortAlgebraic} 0-1";
                        move.LongAlgebraic = $"{move.LongAlgebraic} 0-1";
                    }
                }

                await StopPondering();
                return true;
            }

            return false;
        } // Resign

        /// <summary>
        /// Get the FEN board position
        /// https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
        /// </summary>
        /// <returns></returns>
        public string GetFenString()
        {
            StringBuilder sb = new StringBuilder();

            // Board
            sb.Append(Board.GetFenString());

            // Turn
            sb.Append(" ");
            sb.Append(ToMove == Colors.White ? "w" : "b");

            // Castling
            sb.Append(" ");
            if (QueenCastling.Count == 0 && KingCastling.Count == 0) {
                sb.Append("-");
            } else {
                if (KingCastling.Contains(Colors.White))
                    sb.Append("K");
                if (QueenCastling.Contains(Colors.White))
                    sb.Append("Q");

                if (KingCastling.Contains(Colors.Black))
                    sb.Append("k");
                if (QueenCastling.Contains(Colors.Black))
                    sb.Append("q");
            }

            // En passant
            sb.Append(" ");
            if (string.IsNullOrEmpty(EnPassant))
                sb.Append("-");
            else
                sb.Append(EnPassant.ToLower());

            // Halfmove clock
            sb.Append($" {HalfmoveClock}");

            // Fullmove number
            sb.Append($" {(FullmoveNumber == 0 ? 1 : FullmoveNumber)}");

            return sb.ToString();
        } // GetFenString

        /// <summary>
        /// Get available squares to move for a piece from a a starting square.
        /// </summary>
        /// <param name="startSquare">The starting square</param>
        /// <returns>A list of <see cref="Board.Square"/></returns>
        public List<Board.Square> GetAvailableSquares(Board.Square startSquare)
        {
            var res =  GetAvailableSquaresPrimitive(startSquare, false);
            // Check for check
            foreach (var square in new List<Board.Square>(res)) {
                if (square.Notation == startSquare.Notation)
                    continue;

                Board.Square enPassantSquare = null;
                Piece enPassantCapture = null;

                var toSquare = Board.GetSquare(square.Notation);

                bool wasMoved = startSquare.Piece.Moved;
                var toSquarePiece = toSquare.Piece;

                toSquare.Piece = startSquare.Piece;
                toSquare.Piece.Moved = true;
                startSquare.Piece = null;

                // EnPassant capture
                if (toSquare.Notation == EnPassant && toSquare.Piece.Type == Piece.Pieces.Pawn) {
                    if (ToMove == Colors.White)
                        enPassantSquare = Board.GetSquare($"{toSquare.File}{toSquare.Rank - 1}");
                    else
                        enPassantSquare = Board.GetSquare($"{toSquare.File}{toSquare.Rank + 1}");
                    enPassantCapture = enPassantSquare.Piece;
                    enPassantSquare.Piece = null;
                }

                var kingSquare = Board.GetKingSquare(toSquare.Piece.Color);
                if (IsAttacked(kingSquare, kingSquare.Piece.Color))
                    res.Remove(square);

                // Revert move
                startSquare.Piece = toSquare.Piece;
                startSquare.Piece.Moved = wasMoved;
                toSquare.Piece = toSquarePiece;
                if (enPassantSquare != null)
                    enPassantSquare.Piece = enPassantCapture;
            }
            return res;
        } // GetAvailableSquares

        /// <summary>
        /// Check if a <see cref="Board.Square"/> is attacked by a piece of the opposite color of the given one
        /// </summary>
        /// <param name="square">The <see cref="Board.Square"/> to check</param>
        /// <param name="pieceColor">The color</param>
        /// <returns></returns>
        public bool IsAttacked(Board.Square square, Game.Colors pieceColor)
        {
            foreach (var f in Board.Squares) {
                if (f.Piece != null && f.Piece.Color != pieceColor) {
                    var af = GetAvailableSquaresPrimitive(f, true);
                    if (af.Contains(square))
                        return true;
                }
            }
            return false;
        } // IsAttacked

        /// <summary>
        /// Check if the given color is in checkmate
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public async Task<bool> IsCheckmate(Colors color)
        {
            var kingSquare = Board.GetKingSquare(color);
            if (IsAttacked(kingSquare, color))
                return !await HasValidMoves(color);

            return false;
        } // IsCheckmate

        /// <summary>
        /// Check if the given color is in stall
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public async Task<bool> IsStalemate(Colors color)
        {
            var kingSquare = Board.GetKingSquare(color);
            if (!IsAttacked(kingSquare, color))
                return !await HasValidMoves(color);

            return false;
        } // IsStalemate

        public bool IsDraw()
        {
            // Position repetition
            string fen = Board.GetFenString();
            int count;
            if (Settings.DrawForRepetition > 0 && Positions.TryGetValue(fen, out count)) {
                if (count >= Settings.DrawForRepetition)
                    return true;
            }

            var white = Board.GetPieces(Colors.White);
            var black  = Board.GetPieces(Colors.Black);

            // King vs King
            if (white.Count == 1 && black.Count == 1)
                return true;

            // King and Bishop vs King
            if ((white.Count == 1 && black.Count == 2 && black.Where(p => p.Type == Piece.Pieces.Bishop).Count() == 1) ||
                (black.Count == 1 && white.Count == 2 && white.Where(p => p.Type == Piece.Pieces.Bishop).Count() == 1))
                return true;

            // King and Knight vs King
            if ((white.Count == 1 && black.Count == 2 && black.Where(p => p.Type == Piece.Pieces.Knight).Count() == 1) ||
                (black.Count == 1 && white.Count == 2 && white.Where(p => p.Type == Piece.Pieces.Knight).Count() == 1))
                return true;

            // King and Bishop vs King and Bishop with bishops on the same color
            if (white.Count == 2 && black.Count == 2) {
                var bBishop = black.Where(p => p.Type == Piece.Pieces.Bishop).FirstOrDefault();
                var wBishop = white.Where(p => p.Type == Piece.Pieces.Bishop).FirstOrDefault();
                if (bBishop != null && wBishop != null && bBishop.InitialSquareColor == wBishop.InitialSquareColor)
                    return true;
            }
            return false;
        } // IsDraw

        /// <summary>
        /// Save the current game to a file
        /// </summary>
        /// <param name="file">The file path</param>
        /// <returns></returns>
        public async Task<bool> Save(string file)
        {
            using (Stream sw = new FileStream(file, FileMode.Create, FileAccess.Write)) {
                Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                string content = JsonConvert.SerializeObject(this, m_SerializerSettings);
                byte[] data = Zip(content);
                await sw.WriteAsync(data, 0, data.Length);
            }
            FileName = file;
            return true;
        } // Save

        /// <summary>
        /// Load a game from a file
        /// </summary>
        /// <param name="file">The file path</param>
        /// <returns></returns>
        public static async Task<Game> Load(string file)
        {
            using (Stream sr = new FileStream(file, FileMode.Open, FileAccess.Read)) {
                byte[] data = new byte[sr.Length];
                await sr.ReadAsync(data, 0, data.Length);
                string str = UnZip(data);
                var res = JsonConvert.DeserializeObject<Game>(str, m_SerializerSettings);
                if (res == null)
                    throw new Exception("Failed to load file");

                res.FileName = file;
                return res;
            }
        } // Load

        /// <summary>
        /// Save the current game to a file in PGN format
        /// </summary>
        /// <param name="file">The file path</param>
        /// <returns></returns>
        public async Task<bool> SaveToPgn(string file)
        {
            PGN pgn = new PGN()
            {
                Event = Settings.Event,
                Site = "?",
                Date = Settings.Date == null ? "??" : Settings.Date.Value.ToString("yyyy.MM.dd"),
                Round = "?",
                White = Settings.WhitePlayerName,
                Black = Settings.BlackPlayerName,
                ECO = ECO
            };

            pgn.WhiteElo = GetPlayer(Colors.White)?.Elo;
            pgn.BlackElo = GetPlayer(Colors.Black)?.Elo;

            string result = "*";
            string termination = string.Empty;
            if (Ended) {
                termination = "normal";
                if (Result == Results.Draw) {
                    result = "1/2-1/2";
                } else if (Result == Results.Aborted) {
                    result = "*";
                } else {
                    if (Winner == Colors.White)
                        result = "1-0";
                    else
                        result = "0-1";

                    switch (Result) {
                        case Results.Resignation:
                            termination = "abandoned";
                            break;
                        case Results.Timeout:
                            termination = "time forfeit";
                            break;
                    }
                }
            }

            pgn.Result = result;
            pgn.Termination = termination;

            if (InitialFenPosition != StandardInitialFenPosition)
                pgn.FEN = InitialFenPosition;

            foreach (var m in Moves) {
                pgn.Moves.Add(new PGN.Move() { Notation = m.ShortAlgebraic, Comment = m.Comment });
            }

            return await pgn.Save(file);
        } // SaveToPgn

        /// <summary>
        /// Load a game from a file in PGN format
        /// </summary>
        /// <param name="file">The file path</param>
        /// <returns></returns>
        public static async Task<Game> LoadFromPgn(PGN pgn)
        {
            Game res = new Game() {
              ECO = pgn.ECO
            };

            Game.GameSettings settings = new GameSettings()
            {
                Event = pgn.Event,
                Date = pgn.GetDate(),
                InitialFenPosition = pgn.FEN
            };
            settings.Players.Add(new HumanPlayer(Colors.White, pgn.White, pgn.WhiteElo));
            settings.Players.Add(new HumanPlayer(Colors.Black, pgn.Black, pgn.BlackElo));
            res.Init(settings);

            foreach (var m in pgn.Moves) {
                char promotion;
                string annotation;
                string an = res.GetCoordinateNotation(res.ToMove, m.Notation, out promotion, out annotation);

                var movedPieces = await res.DoMove(an, false, true);
                if (promotion != '\0')
                    movedPieces[0].Piece.Type = Piece.GetTypeFromAcronym(promotion);

                if (!string.IsNullOrEmpty(m.Comment))
                    res.Moves.Last().Comment = m.Comment;

                if (!string.IsNullOrEmpty(annotation))
                    res.Moves.Last().Annotation = annotation;
            }

            if (pgn.Result == "1-0") {
                res.Status = Statuses.Ended;
                res.Result = Results.Checkmate;
                res.Winner = Colors.White;
            } else if (pgn.Result == "0-1") {
                res.Status = Statuses.Ended;
                res.Result = Results.Checkmate;
                res.Winner = Colors.Black;
            } else if (pgn.Result == "1/2-1/2") {
                res.Status = Statuses.Ended;
                res.Result = Results.Draw;
            } else if (pgn.Result == "*") {
                res.Status = Statuses.Ended;
                res.Result = Results.Aborted;
            }

            if (!string.IsNullOrEmpty(pgn.Result)) {
                var lm = res.Moves.Last();
                lm.ShortAlgebraic = $"{lm.ShortAlgebraic} {pgn.Result}";
                lm.LongAlgebraic = $"{lm.LongAlgebraic} {pgn.Result}";
            }
            return res;
        } // LoadFromPgn

        public string GetCoordinateNotation(Colors color, string shortAlgebraicNotation, out char promotion, out string annotation)
        {
            promotion = '\0';
            annotation = string.Empty;

            string toSquareNotation = string.Empty;
            List<Piece> allPieces = Board.GetPieces(color);
            List<Piece> movePieces = null;

            // Annotation
            int qIdx = shortAlgebraicNotation.IndexOf("?");
            int eIdx = shortAlgebraicNotation.IndexOf("!");
            if (qIdx >= 0 || eIdx >= 0) {
                int idx = qIdx >= 0 && eIdx >= 0 ? Math.Min(qIdx, eIdx) : qIdx >= 0 ? qIdx : eIdx;
                annotation = shortAlgebraicNotation.Substring(idx);
                shortAlgebraicNotation = shortAlgebraicNotation.Remove(idx);
            }

            // Castling
            if (shortAlgebraicNotation.StartsWith("0-0-0") || shortAlgebraicNotation.StartsWith("O-O-O")) {
                // Queen  castling
                return color == Colors.White ? WhiteQueenCastlingMove : BlackQueenCastlingMove;
            } else if (shortAlgebraicNotation.StartsWith("0-0") || shortAlgebraicNotation.StartsWith("O-O")) {
                // King castling
                return color == Colors.White ? WhiteKingCastlingMove : BlackKingCastlingMove;
            }

            // Remove the "x"
            int xIdx = shortAlgebraicNotation.IndexOf("x");
            if (xIdx >= 0)
                shortAlgebraicNotation = shortAlgebraicNotation.Remove(xIdx, 1);
            // Remove the "+"
            if (shortAlgebraicNotation.EndsWith("+"))
                shortAlgebraicNotation = shortAlgebraicNotation.Remove(shortAlgebraicNotation.Length - 1, 1);
            // Remove the "#"
            if (shortAlgebraicNotation.EndsWith("#"))
                shortAlgebraicNotation = shortAlgebraicNotation.Remove(shortAlgebraicNotation.Length - 1, 1);
            // Remove the "="
            xIdx = shortAlgebraicNotation.IndexOf("=");
            if (xIdx >= 0)
                shortAlgebraicNotation = shortAlgebraicNotation.Remove(xIdx, 1);

            // Promotion
            if (char.IsLetter(shortAlgebraicNotation[shortAlgebraicNotation.Length - 1])) {
                promotion = shortAlgebraicNotation[shortAlgebraicNotation.Length - 1];
                shortAlgebraicNotation = shortAlgebraicNotation.Remove(shortAlgebraicNotation.Length - 1);
            }

            if (shortAlgebraicNotation.Length == 2) {
                // Pawn (e4)
                movePieces = allPieces.Where(p => p.Type == Piece.Pieces.Pawn).ToList();
                // Also add other pieces after pawns (in some PGN you can find moves in this format when only one piece can do that move)
                movePieces.AddRange(allPieces.Where(p => p.Type != Piece.Pieces.Pawn));
                toSquareNotation = shortAlgebraicNotation;
            } else if (shortAlgebraicNotation.Length == 3 && char.IsLower(shortAlgebraicNotation[0])) {
                // Pawn capture (fe4)
                movePieces = allPieces.Where(p => p.Type == Piece.Pieces.Pawn && Board.GetSquare(p).File == char.ToUpper(shortAlgebraicNotation[0])).ToList();
                toSquareNotation = shortAlgebraicNotation.Remove(0, 1);
            } else if (char.IsUpper(shortAlgebraicNotation[0])) {
                // Piece (Nf3, Ngf3, N1f3, Ng1f3)
                var pieceType = Piece.GetTypeFromAcronym(shortAlgebraicNotation[0]);
                movePieces = allPieces.Where(p => p.Type == pieceType).ToList();

                if (shortAlgebraicNotation.Length == 3) {
                    // Normal (Nf3)
                    toSquareNotation = shortAlgebraicNotation.Substring(1);
                } else if (shortAlgebraicNotation.Length == 4) {
                    if (char.IsLetter(shortAlgebraicNotation[1])){
                        // File disambiguation (Ngf3)
                        movePieces = movePieces.Where(p => Board.GetSquare(p).File == char.ToUpper(shortAlgebraicNotation[1])).ToList();
                    } else {
                        // Rank disambiguation (N1f3)
                        movePieces = movePieces.Where(p => Board.GetSquare(p).Rank == int.Parse(shortAlgebraicNotation[1].ToString())).ToList();
                    }
                    toSquareNotation = shortAlgebraicNotation.Substring(2);
                } else if (shortAlgebraicNotation.Length == 5) {
                    // Disambiguation (Ng1f3)
                    movePieces = movePieces.Where(p => Board.GetSquare(p).Notation == shortAlgebraicNotation.Substring(1, 2).ToUpper()).ToList();
                    toSquareNotation = shortAlgebraicNotation.Substring(3);
                }
            } else if (shortAlgebraicNotation.Length == 4) {
                return shortAlgebraicNotation;
            }

            if (movePieces != null) {
                shortAlgebraicNotation = toSquareNotation.ToUpper();
                foreach (var p in movePieces) {
                    var square = Board.GetSquare(p);
                    if (square != null) {
                        foreach (var to in GetAvailableSquares(square)) {
                            if (to.Notation == shortAlgebraicNotation)
                                return $"{square.Notation.ToLower()}{to.Notation.ToLower()}";
                        }
                    }
                }
            }
            return string.Empty;
        } // GetCoordinateNotation

        /// <summary>
        /// Analyze the game
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<List<Engines.EngineBase.AnalyzeResult>> Analyze(Engines.EngineBase engine, int? depth, Action<int, int> progress, CancellationToken token)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            var res = new List<Engines.EngineBase.AnalyzeResult>() {
                new Engines.EngineBase.AnalyzeResult(Colors.White, new Engines.EngineBase.Info.ScoreValue())
            };

            if (engine.SupportAnalyze()) {
                await engine.Start();
                await engine.ApplyOptions(true);
                await engine.EnterAnalyzeMode();

                int idx = 0;
                List<MoveNotation> moves = new List<MoveNotation>(Moves);
                if (Result == Results.Checkmate || Result == Results.Stalemate)
                    moves.RemoveAt(moves.Count - 1);

                foreach(var move in moves) {
                    if (token.IsCancellationRequested)
                        break;

                    var a = await engine.Analyze(move.Fen, depth, null, token);
                    if (a != null) {
                        res.Add(a);
                        progress?.Invoke(++idx, moves.Count);
                    }
                }

                await engine.ExitAnalyzeMode();
                await engine.Stop();
            }

            if (!token.IsCancellationRequested)
                AnalyzeResults = res;
            return res;
        } // Analyze

        public Player GetPlayer(Colors color)
        {
            return Settings?.Players.Where(p => p.Color == color)
            .FirstOrDefault();
        } // GetPlayer

        public async Task<bool> StopPondering()
        {
            if (m_PonderTask != null) {
                await m_PonderPlayer.Engine.StopCommand();
                await m_PonderTask;
                m_PonderTask.Dispose();
                m_PonderTask = null;
                m_PonderPlayer = null;
            }
            return true;
        } // StopPondering

        public Game Copy()
        {
            var sSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects };
            return JsonConvert.DeserializeObject<Game>(JsonConvert.SerializeObject(this, sSettings), sSettings);
        } // Copy

        #region private operations
        private async Task<bool> InitEnginePlayers()
        {
            foreach (var p in Settings.Players) {
                if (p is EnginePlayer) {
                    var ep = p as EnginePlayer;
                    if (ep.Engine == null)
                        throw new Exception("Engine player has no engine");

                    // Start engine
                    ep.Engine.PlayAs = ep.Color;
                    await ep.Engine.Start();
                    ep.Engine.Error += (s, args) =>
                    {
                        EngineError?.Invoke(this, args);
                    };

                    // Chess960 option
                    var chess960Option = ep.Engine.GetChess960Option();
                    if (chess960Option != null)
                        chess960Option.Value = Settings.IsChess960 ? "true" : "false";

                    if (ep.Engine is Engines.Uci && !string.IsNullOrEmpty(ep.Personality)) {
                        var pOpt = ep.Engine.GetOption(Engines.Uci.PersonalityOptionNames);
                        if (pOpt != null) {
                            pOpt.Value = ep.Personality;
                        }
                    }
                    await ep.Engine.ApplyOptions(true);
                    await ep.Engine.NewGame(
                        Settings.MaximumTime.HasValue ? (int)Settings.MaximumTime.Value.TotalMinutes : 0,
                        Settings.TimeIncrement.HasValue ? (int)Settings.TimeIncrement.Value.TotalSeconds : 0);

                    // Set The King personality
                    if (ep.Engine is Engines.TheKing && ep.TheKingPersonality != null) {
                        var tk = ep.Engine as Engines.TheKing;
                        await tk.ApplyPersonality(ep.TheKingPersonality);
                        var tkOpt = tk.GetOption(Engines.TheKing.OpeningBooksFolderOptionName)?.Value;
                        if (!string.IsNullOrEmpty(tkOpt) && !string.IsNullOrEmpty(ep.TheKingPersonality.OpeningBook))
                            ep.OpeningBookFileName = Path.Combine(tkOpt, ep.TheKingPersonality.OpeningBook);
                    }

                    if (ep.Engine is Engines.Cecp)
                        await ((Engines.Cecp)ep.Engine).SetBoard(GetFenString());

                    // Load opening book
                    if (!string.IsNullOrEmpty(ep.OpeningBookFileName))
                        ep.OpeningBook = Books.BookFactory.OpenBook(ep.OpeningBookFileName);
                }
            }
            return true;
        } // InitEnginePlayers

        private async Task<Engines.EngineBase.BestMove> PonderMove(EnginePlayer enginePlayer, string move, Action<Engines.EngineBase, string> outputCallback = null)
        {
            m_PonderingMove = move;
            m_PonderPlayer = enginePlayer;
            List<string> moves = new List<string>();
            foreach (var m in Moves)
                moves.Add(m.Coordinate);
            moves.Add(move);
            await enginePlayer.Engine.SetPosition(InitialFenPosition, moves);

            CancellationTokenSource cts = new CancellationTokenSource();
            var res = await enginePlayer.Engine.Ponder(WhiteTimeLeftMilliSecs ?? 0, WhiteIncrementMillisecs, BlackTimeLeftMilliSecs ?? 0,
                BlackIncrementMillisecs, Settings.EngineDepth, cts.Token,
                (output) => outputCallback(enginePlayer.Engine, output));
            cts.Dispose();

            m_PonderingMove = string.Empty;
            return res;
        } // PonderMove

        private async Task<bool> HasValidMoves(Game.Colors color)
        {
            // Copy the game to check moves
            var tempGame = JsonConvert.DeserializeObject<Game>(JsonConvert.SerializeObject(this, m_SerializerSettings), m_SerializerSettings);
            tempGame.ToMove = color;
            foreach (var square in Board.Squares) {
                if (square.Piece?.Color == color) {
                    var squares = GetAvailableSquares(square);
                    foreach (var toSquare in squares) {
                        try {
                            await tempGame.DoMove($"{square.Notation}{toSquare.Notation}", false);
                            return true;
                        } catch {
                        }
                    }
                }
            }
            return false;
        } // HasValidMoves

        private async void OnBlackTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            BlackTimeMilliSecs += 100;
            BlackTimeLeftMilliSecs -= 100;
            // Sudden death
            if (BlackTimeLeftMilliSecs <= 0 && (!Settings.TrainingMode || Settings.HumanPlayerColor != Colors.Black)) {
                m_WhiteTimer.Stop();
                m_BlackTimer.Stop();

                BlackTimeLeftMilliSecs = 0;
                Status = Statuses.Ended;
                Result = Results.Timeout;
                Winner = Colors.White;
                await StopPondering();

                var move = Moves.Last();
                if (move != null) {
                    move.ShortAlgebraic = $"{move.ShortAlgebraic} 1-0";
                    move.LongAlgebraic = $"{move.LongAlgebraic} 1-0";
                }
            }
            BlackTimer?.Invoke(this, new EventArgs());
        } // BlackTimerElapsed

        private async void OnWhiteTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            WhiteTimeMilliSecs += 100;
            WhiteTimeLeftMilliSecs -= 100;
            // Sudden death
            if (WhiteTimeLeftMilliSecs <= 0 && (!Settings.TrainingMode || Settings.HumanPlayerColor != Colors.White)) {
                m_WhiteTimer.Stop();
                m_BlackTimer.Stop();

                WhiteTimeLeftMilliSecs = 0;
                Status = Statuses.Ended;
                Result = Results.Timeout;
                Winner = Colors.Black;
                await StopPondering();

                var move = Moves.Last();
                if (move != null) {
                    move.ShortAlgebraic = $"{move.ShortAlgebraic} 0-1";
                    move.LongAlgebraic = $"{move.LongAlgebraic} 0-1";
                }
            }
            WhiteTimer?.Invoke(this, new EventArgs());
        } // WhiteTimerElapsed

        /// <summary>
        /// Return all the available squares for a piece.
        /// This mehtod does not check for king check.
        /// </summary>
        /// <param name="startSquare">The <see cref="Piece"/> <see cref="Board.Square"/></param>
        /// <param name="avoidCastling"></param>
        /// <returns></returns>
        private List<Board.Square> GetAvailableSquaresPrimitive(Board.Square startSquare, bool avoidCastling)
        {
            var res = new List<Board.Square>();
            if (startSquare.Piece != null) {
                switch (startSquare.Piece.Type) {
                    // PAWNS
                    case Piece.Pieces.Pawn:
                        res.AddRange(GetAvailableSquaresPawn(startSquare));
                        break;
                    // KNIGHT
                    case Piece.Pieces.Knight:
                        res.AddRange(GetAvailableSquaresKnight(startSquare));
                        break;
                    // BISHOP
                    case Piece.Pieces.Bishop:
                        res.AddRange(GetAvailableSquaresBishop(startSquare));
                        break;
                    // ROOK
                    case Piece.Pieces.Rook:
                        res.AddRange(GetAvailableSquaresRook(startSquare));
                        break;
                    // QUEEN
                    case Piece.Pieces.Queen:
                        res.AddRange(GetAvailableSquaresBishop(startSquare));
                        res.AddRange(GetAvailableSquaresRook(startSquare));
                        break;
                    // KING
                    case Piece.Pieces.King:
                        res.AddRange(GetAvailableSquaresKing(startSquare, avoidCastling));
                        break;
                }
            }
            return res;
        } // GetAvailableSquaresPrimitive

        /// <summary>
        /// Check if it is possible to castle from a square to another
        /// Check that the squaress are empty and not under attack of other pieces
        /// </summary>
        /// <param name="color">The King color</param>
        /// <param name="fromFile">The file from</param>
        /// <param name="toFile">The file to</param>
        /// <returns>True or false</returns>
        private bool CanCastle(Colors color, char fromFile, char toFile)
        {
            fromFile = char.ToUpper(fromFile);
            toFile = char.ToUpper(toFile);

            int rank = color == Colors.White ? 1 : 8;
            int idxFrom = 0;
            int idxTo = 0;
            char rookTargetFile;

            // If the king is in check it cannot castle
            if (IsAttacked(Board.GetSquare($"{fromFile}{rank}"), color))
                return false;

            if (fromFile < toFile) {
                idxFrom = Board.Files.IndexOf(char.ToUpper(fromFile)) + 1;
                idxTo = Board.Files.IndexOf(char.ToUpper(toFile)) + 1;
                rookTargetFile = Board.GetPreviousFile(toFile).Value;
            } else {
                idxFrom = Board.Files.IndexOf(char.ToUpper(toFile));
                idxTo = Board.Files.IndexOf(char.ToUpper(fromFile));
                rookTargetFile = Board.GetNextFile(toFile).Value;
            }

            // Check squares the king must travel
            for (int i = idxFrom; i < idxTo; i++){
                var square = Board.GetSquare($"{Board.Files[i]}{rank}");
                if (square.Piece != null) {
                    if (Settings.IsChess960 && (square.Piece.Color != color || (square.Piece.Type != Piece.Pieces.King && square.Piece.Type != Piece.Pieces.Rook)))
                        return false;
                    else if (!Settings.IsChess960)
                        return false;
                }
                if (IsAttacked(square, color))
                    return false;
            }

            var targetRookSquare = Board.GetSquare($"{rookTargetFile}{rank}");
            if (targetRookSquare.Piece != null && targetRookSquare.Piece.Type != Piece.Pieces.King)
                return false;

            // Check squares the rook must travel
            if (fromFile < toFile) {
                var rookSquare = GetKingRook(color);
                if (rookSquare == null)
                    return false;
                idxFrom = Board.Files.IndexOf(rookSquare.File);
                idxTo = Board.Files.IndexOf(rookTargetFile);
            } else {
                var rookSquare = GetQueenRook(color);
                if (rookSquare == null)
                    return false;
                idxFrom = Board.Files.IndexOf(rookSquare.File) + 1;
                idxTo = Board.Files.IndexOf(rookTargetFile);
            }
            for (int i = idxFrom; i < idxTo; i++) {
                var square = Board.GetSquare($"{Board.Files[i]}{rank}");
                if (square.Piece != null) {
                    if (Settings.IsChess960 && (square.Piece.Color != color || (square.Piece.Type != Piece.Pieces.King && square.Piece.Type != Piece.Pieces.Rook)))
                        return false;
                    else if (!Settings.IsChess960)
                        return false;
                }
            }
            return true;
        } // CanCastle

        /// <summary>
        /// Return the square with the king rook for the given color
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private Board.Square GetKingRook(Game.Colors color)
        {
            return Board.Squares.Where(s => s.Piece != null && !s.Piece.Moved && s.Piece.Type == Piece.Pieces.Rook
                && s.Piece.Color == color && s.File > 'E').FirstOrDefault();
        } // GetKingRook

        /// <summary>
        /// Return the square with the queen rook for the given color
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private Board.Square GetQueenRook(Game.Colors color)
        {
            return Board.Squares.Where(s => s.Piece != null && !s.Piece.Moved && s.Piece.Type == Piece.Pieces.Rook
                && s.Piece.Color == color && s.File < 'E').FirstOrDefault();
        } // GetQueenRook

        private List<Board.Square> GetAvailableSquaresPawn(Board.Square startSquare)
        {
            var res = new List<Board.Square>();
            if (startSquare.Piece != null) {
                var piece = startSquare.Piece;
                Board.Square nextSquare = null;
                char? nextFile = null;
                char? prevFile = null;

                // Normal moves
                if (piece.Color == Colors.White && startSquare.Rank < 8)
                    nextSquare = Board.GetSquare($"{startSquare.File}{startSquare.Rank + 1}");
                else if (piece.Color == Colors.Black && startSquare.Rank > 1)
                    nextSquare = Board.GetSquare($"{startSquare.File}{startSquare.Rank - 1}");
                if (nextSquare != null && nextSquare.Piece == null)
                    res.Add(nextSquare);

                if (!piece.Moved) {
                    if (piece.Color == Colors.White && Board.GetSquare($"{startSquare.File}{startSquare.Rank + 1}").Piece == null && startSquare.Rank + 2 <= 8)
                        nextSquare = Board.GetSquare($"{startSquare.File}{startSquare.Rank + 2}");
                    else if (piece.Color == Colors.Black && Board.GetSquare($"{startSquare.File}{startSquare.Rank - 1}").Piece == null && startSquare.Rank - 2 >= 1)
                        nextSquare = Board.GetSquare($"{startSquare.File}{startSquare.Rank - 2}");

                    if (nextSquare?.Piece == null)
                        res.Add(nextSquare);
                }

                // Capture
                nextFile = Board.GetNextFile(startSquare.File);
                if (nextFile != null) {
                    if (piece.Color == Colors.White && startSquare.Rank < 8)
                        nextSquare = Board.GetSquare($"{nextFile}{startSquare.Rank + 1}");
                    else if (piece.Color == Colors.Black && startSquare.Rank > 1)
                        nextSquare = Board.GetSquare($"{nextFile}{startSquare.Rank - 1}");

                    if (nextSquare != null && nextSquare.Piece != null && nextSquare.Piece.Color != piece.Color)
                        res.Add(nextSquare);
                }

                prevFile = Board.GetPreviousFile(startSquare.File);
                if (prevFile != null) {
                    if (piece.Color == Colors.White && startSquare.Rank < 8)
                        nextSquare = Board.GetSquare($"{prevFile}{startSquare.Rank + 1}");
                    else if(piece.Color == Colors.Black && startSquare.Rank > 1)
                        nextSquare = Board.GetSquare($"{prevFile}{startSquare.Rank - 1}");

                    if (nextSquare != null && nextSquare.Piece != null && nextSquare.Piece.Color != piece.Color)
                        res.Add(nextSquare);
                }

                // En passant
                if (!string.IsNullOrEmpty(EnPassant)) {
                    var enPassantSquare = Board.GetSquare(EnPassant);
                    if (enPassantSquare.Piece == null && (enPassantSquare.File == prevFile || enPassantSquare.File == nextFile)) {
                        if (piece.Color == Colors.White && enPassantSquare.Rank == startSquare.Rank + 1)
                            res.Add(enPassantSquare);
                        else if (piece.Color == Colors.Black && enPassantSquare.Rank == startSquare.Rank - 1)
                            res.Add(enPassantSquare);
                    }
                }
            }
            return res;
        } // GetAvailableSquaresPawn

        private List<Board.Square> GetAvailableSquaresKnight(Board.Square startSquare)
        {
            var res = new List<Board.Square>();
            if (startSquare.Piece != null) {
                var piece = startSquare.Piece;
                Board.Square nextSquare = null;
                char? nextFile = null;
                char? prevFile = null;

                nextFile = Board.GetNextFile(startSquare.File);
                if (nextFile != null) {
                    nextSquare = startSquare.Rank > 2 ? Board.GetSquare($"{nextFile}{startSquare.Rank - 2}") : null;
                    if (nextSquare != null && (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color))
                        res.Add(nextSquare);
                    nextSquare = startSquare.Rank < 7 ? Board.GetSquare($"{nextFile}{startSquare.Rank + 2}") : null;
                    if (nextSquare != null && (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color))
                        res.Add(nextSquare);

                    nextFile = Board.GetNextFile(nextFile.Value);
                    if (nextFile != null) {
                        nextSquare = startSquare.Rank > 1 ? Board.GetSquare($"{nextFile}{startSquare.Rank - 1}") : null;
                        if (nextSquare != null && (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color))
                            res.Add(nextSquare);
                        nextSquare = startSquare.Rank < 8 ? Board.GetSquare($"{nextFile}{startSquare.Rank + 1}") : null;
                        if (nextSquare != null && (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color))
                            res.Add(nextSquare);
                    }
                }

                prevFile = Board.GetPreviousFile(startSquare.File);
                if (prevFile != null) {
                    nextSquare = startSquare.Rank > 2 ? Board.GetSquare($"{prevFile}{startSquare.Rank - 2}") : null;
                    if (nextSquare != null && (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color))
                        res.Add(nextSquare);
                    nextSquare = startSquare.Rank < 7 ? Board.GetSquare($"{prevFile}{startSquare.Rank + 2}") : null;
                    if (nextSquare != null && (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color))
                        res.Add(nextSquare);

                    prevFile = Board.GetPreviousFile(prevFile.Value);
                    if (prevFile != null) {
                        nextSquare = startSquare.Rank > 1 ? Board.GetSquare($"{prevFile}{startSquare.Rank - 1}") : null;
                        if (nextSquare != null && (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color))
                            res.Add(nextSquare);
                        nextSquare = startSquare.Rank < 8 ? Board.GetSquare($"{prevFile}{startSquare.Rank + 1}") : null;
                        if (nextSquare != null && (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color))
                            res.Add(nextSquare);
                    }
                }
            }
            return res;
        } // GetAvailableSquaresKnight

        private List<Board.Square> GetAvailableSquaresKing(Board.Square startSquare, bool avoidCastling)
        {
            var res = new List<Board.Square>();
            if (startSquare.Piece != null) {
                var piece = startSquare.Piece;
                Board.Square nextSquare = null;
                char? nextFile = null;
                char? prevFile = null;

                // Normal moves
                nextFile = Board.GetNextFile(startSquare.File);
                if (nextFile != null) {
                    if (startSquare.Rank <= 7) {
                        nextSquare = Board.GetSquare($"{nextFile}{startSquare.Rank + 1}");
                        if (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color)
                            res.Add(nextSquare);
                    }
                    nextSquare = Board.GetSquare($"{nextFile}{startSquare.Rank}");
                    if (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color)
                        res.Add(nextSquare);
                    if (startSquare.Rank > 1) {
                        nextSquare = Board.GetSquare($"{nextFile}{startSquare.Rank - 1}");
                        if (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color)
                            res.Add(nextSquare);
                    }
                }

                prevFile = Board.GetPreviousFile(startSquare.File);
                if (prevFile != null) {
                    if (startSquare.Rank <= 7) {
                        nextSquare = Board.GetSquare($"{prevFile}{startSquare.Rank + 1}");
                        if (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color)
                            res.Add(nextSquare);
                    }
                    nextSquare = Board.GetSquare($"{prevFile}{startSquare.Rank}");
                    if (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color)
                        res.Add(nextSquare);
                    if (startSquare.Rank > 1) {
                        nextSquare = Board.GetSquare($"{prevFile}{startSquare.Rank - 1}");
                        if (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color)
                            res.Add(nextSquare);
                    }
                }

                if (startSquare.Rank <= 7) {
                    nextSquare = Board.GetSquare($"{startSquare.File}{startSquare.Rank + 1}");
                    if (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color)
                        res.Add(nextSquare);
                }
                if (startSquare.Rank > 1) {
                    nextSquare = Board.GetSquare($"{startSquare.File}{startSquare.Rank - 1}");
                    if (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color)
                        res.Add(nextSquare);
                }

                // Castling
                if (!avoidCastling) {
                    if (KingCastling.Contains(piece.Color)) {
                        if (CanCastle(piece.Color, startSquare.File, 'g'))
                            res.Add(Board.GetSquare($"g{startSquare.Rank}"));
                    }

                    if (QueenCastling.Contains(piece.Color)) {
                        if (CanCastle(piece.Color, startSquare.File, 'c'))
                            res.Add(Board.GetSquare($"c{startSquare.Rank}"));
                    }
                }
            }
            return res;
        } // GetAvailableSquaresKing

        private List<Board.Square> GetAvailableSquaresBishop(Board.Square startSquare)
        {
            var res = new List<Board.Square>();
            if (startSquare.Piece != null) {
                var piece = startSquare.Piece;
                Board.Square nextSquare = null;
                char? nextFile = null;
                char? prevFile = null;

                nextFile = Board.GetNextFile(startSquare.File);
                int? upperRank = startSquare.Rank;
                int? lowerRank = startSquare.Rank;
                while (nextFile != null) {
                    if (upperRank != null) {
                        upperRank++;
                        nextSquare = upperRank <= 8 ? Board.GetSquare($"{nextFile}{upperRank}") : null;
                        if (nextSquare != null && (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color)) {
                            res.Add(nextSquare);
                            if (nextSquare.Piece != null)
                                upperRank = null;
                        } else
                            upperRank = null;
                    }

                    if (lowerRank != null) {
                        lowerRank--;
                        nextSquare = lowerRank >= 1 ? Board.GetSquare($"{nextFile}{lowerRank}") : null;
                        if (nextSquare != null && (nextSquare?.Piece == null || nextSquare?.Piece.Color != piece.Color)) {
                            res.Add(nextSquare);
                            if (nextSquare.Piece != null)
                                lowerRank = null;
                        } else
                            lowerRank = null;
                    }

                    if (lowerRank == null && upperRank == null)
                        break;

                    nextFile = Board.GetNextFile(nextFile.Value);
                }

                prevFile = Board.GetPreviousFile(startSquare.File);
                upperRank = startSquare.Rank;
                lowerRank = startSquare.Rank;
                while (prevFile != null) {
                    if (upperRank != null) {
                        upperRank++;
                        nextSquare = upperRank <= 8 ? Board.GetSquare($"{prevFile}{upperRank}") : null;
                        if (nextSquare != null && (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color)) {
                            res.Add(nextSquare);
                            if (nextSquare.Piece != null)
                                upperRank = null;
                        } else
                            upperRank = null;
                    }

                    if (lowerRank != null) {
                        lowerRank--;
                        nextSquare = lowerRank >= 1 ? Board.GetSquare($"{prevFile}{lowerRank}") : null;
                        if (nextSquare != null && (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color)) {
                            res.Add(nextSquare);
                            if (nextSquare.Piece != null)
                                lowerRank = null;
                        } else
                            lowerRank = null;
                    }
                    prevFile = Board.GetPreviousFile(prevFile.Value);
                }
            }
            return res;
        } // GetAvailableSquaresBishop

        private List<Board.Square> GetAvailableSquaresRook(Board.Square startSquare)
        {
            var res = new List<Board.Square>();
            if (startSquare.Piece != null) {
                var piece = startSquare.Piece;
                Board.Square nextSquare = null;
                char? nextFile = Board.GetNextFile(startSquare.File);
                char? prevFile = Board.GetPreviousFile(startSquare.File);

                while (nextFile != null) {
                    nextSquare = Board.GetSquare($"{nextFile}{startSquare.Rank}");
                    if (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color) {
                        res.Add(nextSquare);
                        if (nextSquare.Piece != null)
                            break;
                    } else
                        break;
                    nextFile = Board.GetNextFile(nextFile.Value);
                }

                while (prevFile != null) {
                    nextSquare = Board.GetSquare($"{prevFile}{startSquare.Rank}");
                    if (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color) {
                        res.Add(nextSquare);
                        if (nextSquare.Piece != null)
                            break;
                    } else
                        break;
                    prevFile = Board.GetPreviousFile(prevFile.Value);
                }

                for (int i = startSquare.Rank + 1; i <= 8; i++) {
                    nextSquare = Board.GetSquare($"{startSquare.File}{i}");
                    if (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color) {
                        res.Add(nextSquare);
                        if (nextSquare.Piece != null)
                            break;
                    } else
                        break;
                }

                for (int i = startSquare.Rank - 1; i >= 1; i--) {
                    nextSquare = Board.GetSquare($"{startSquare.File}{i}");
                    if (nextSquare.Piece == null || nextSquare.Piece.Color != piece.Color) {
                        res.Add(nextSquare);
                        if (nextSquare.Piece != null)
                            break;
                    } else
                        break;
                }
            }
            return res;
        } // GetAvailableSquaresRook

        private MoveResult DoMoveKingCatling(string move)
        {
            var res = new MoveResult();

            if (!KingCastling.Contains(ToMove))
                throw new InvalidMoveException(move, "castling unavailable", GetFenString());

            var kingSquare = Board.GetSquare(ToMove == Colors.White ? WhiteKingCastlingMove.Substring(0, 2) : BlackKingCastlingMove.Substring(0, 2));
            var rookSquare = GetKingRook(ToMove);
            if (kingSquare.Piece == null || rookSquare.Piece == null ||
                kingSquare.Piece.Type != Piece.Pieces.King || rookSquare.Piece.Type != Piece.Pieces.Rook)
                throw new InvalidMoveException(move, "cannot castle", GetFenString());

            var kingToSquare = Board.GetSquare(ToMove == Colors.White ? WhiteKingCastlingMove.Substring(2, 2) : BlackKingCastlingMove.Substring(2, 2));
            var rookToSquare = Board.GetSquare(ToMove == Colors.White ? "f1" : "f8");
            if (!CanCastle(ToMove, kingSquare.File, kingToSquare.File))
                throw new InvalidMoveException(move, "cannot castle", GetFenString());

            kingToSquare.Piece = kingSquare.Piece;
            kingToSquare.Piece.Moved = true;
            if (kingSquare != kingToSquare)
                kingSquare.Piece = null;
            rookToSquare.Piece = rookSquare.Piece;
            rookToSquare.Piece.Moved = true;
            rookSquare.Piece = null;

            EnPassant = string.Empty;
            KingCastling.Remove(ToMove);
            QueenCastling.Remove(ToMove);

            res.Move = ToMove == Colors.White ? WhiteKingCastlingMove : BlackKingCastlingMove;
            res.Moves.Add(new Move(kingToSquare.Piece, kingSquare, kingToSquare) { CoordinateNotation = $"{kingSquare.Notation.ToLower()}{kingToSquare.Notation.ToLower()}" });
            res.Moves.Add(new Move(rookToSquare.Piece, rookSquare, rookToSquare) { CoordinateNotation = $"{rookSquare.Notation.ToLower()}{rookToSquare.Notation.ToLower()}" });

            return res;
        } // DoMoveKingCatling

        private MoveResult DoMoveQueenCatling(string move)
        {
            var res = new MoveResult();

            if (!QueenCastling.Contains(ToMove))
                throw new InvalidMoveException(move, "castling unavailable", GetFenString());

            var kingSquare = Board.GetSquare(ToMove == Colors.White ? WhiteQueenCastlingMove.Substring(0, 2) : BlackQueenCastlingMove.Substring(0, 2));
            var rookSquare = GetQueenRook(ToMove);
            if (kingSquare.Piece == null || rookSquare.Piece == null ||
                kingSquare.Piece.Type != Piece.Pieces.King || rookSquare.Piece.Type != Piece.Pieces.Rook)
                throw new InvalidMoveException(move, "cannot castle", GetFenString());

            var kingToSquare = Board.GetSquare(ToMove == Colors.White ? "c1" : "c8");
            var rookToSquare = Board.GetSquare(ToMove == Colors.White ? "d1" : "d8");
            if (!CanCastle(ToMove, kingSquare.File, kingToSquare.File))
                throw new InvalidMoveException(move, "cannot castle", GetFenString());

            kingToSquare.Piece = kingSquare.Piece;
            kingToSquare.Piece.Moved = true;
            if (kingSquare != kingToSquare)
                kingSquare.Piece = null;
            rookToSquare.Piece = rookSquare.Piece;
            rookToSquare.Piece.Moved = true;
            rookSquare.Piece = null;

            EnPassant = string.Empty;
            KingCastling.Remove(ToMove);
            QueenCastling.Remove(ToMove);

            res.Move = ToMove == Colors.White ? WhiteQueenCastlingMove : BlackQueenCastlingMove;
            res.Moves.Add(new Move(kingToSquare.Piece, kingSquare, kingToSquare) { CoordinateNotation = $"{kingSquare.Notation.ToLower()}{kingToSquare.Notation.ToLower()}" });
            res.Moves.Add(new Move(rookToSquare.Piece, rookSquare, rookToSquare) { CoordinateNotation = $"{rookSquare.Notation.ToLower()}{rookToSquare.Notation.ToLower()}" });

            return res;
        } // DoMoveQueenCatling

        private async Task<MoveResult> DoMoveNormal(string move, bool skipAvailableSquaresCheck)
        {
            var res = new MoveResult();
            // A move is always 4 bytes or 5 bytes (promotion)
            if (move.Length != 4 && move.Length != 5)
                throw new InvalidMoveException(move, "invalid notation", GetFenString());

            Piece captured = null;
            var fromSquare = Board.GetSquare(move.Substring(0, 2));
            if (fromSquare.Piece == null || fromSquare.Piece.Color != ToMove)
                throw new InvalidMoveException(move, "invalid source square", GetFenString());
            var toSquare = Board.GetSquare(move.Substring(2, 2));
            if (toSquare.Piece != null && toSquare.Piece.Color == fromSquare.Piece.Color)
                throw new InvalidMoveException(move, "invalid destination square", GetFenString());

            // Check if the move is valid
            var validSquares = skipAvailableSquaresCheck ? null : GetAvailableSquares(fromSquare);
            if (validSquares != null && !validSquares.Contains(toSquare))
                throw new InvalidMoveException(move, "invalid move for piece", GetFenString());

            // Check promotion in move
            if (move.Length == 5) {
                string piece = move.Substring(4, 1);
                if (piece != "n" && piece != "b" && piece != "r" && piece != "q")
                    throw new InvalidMoveException(move, "invalid promotion", GetFenString());

                if (fromSquare.Piece.Type != Piece.Pieces.Pawn)
                    throw new InvalidMoveException(move, "only pawns can be promoted", GetFenString());

                if (fromSquare.Piece.Color == Colors.White && toSquare.Rank != 8)
                    throw new InvalidMoveException(move, "invalid promotion", GetFenString());
                if (fromSquare.Piece.Color == Colors.Black && toSquare.Rank != 1)
                    throw new InvalidMoveException(move, "invalid promotion", GetFenString());

                fromSquare.Piece.Type = Piece.GetTypeFromAcronym(piece[0]);
                move = move.Remove(move.Length - 1, 1);
                move = $"{move}{fromSquare.Piece.Acronym}";
                res.Promoted = true;
            }

            // En passant capture
            Board.Square enPassantPawnSquare = null;
            Piece enPassantPawn = null;
            if (toSquare.Notation == EnPassant && fromSquare.Piece.Type == Piece.Pieces.Pawn) {
                if (ToMove == Colors.White)
                    enPassantPawnSquare = Board.GetSquare($"{toSquare.File}{toSquare.Rank - 1}");
                else
                    enPassantPawnSquare = Board.GetSquare($"{toSquare.File}{toSquare.Rank + 1}");
                enPassantPawn = enPassantPawnSquare.Piece;
                captured = enPassantPawnSquare.Piece;
                enPassantPawnSquare.Piece = null;
            } else if (toSquare.Piece != null) {
                // Capture the piece
                captured = toSquare.Piece;
            }

            // Before moving the piece check for disambiguations for algebraic notation
            List<Board.Square> samePieces = this.Board.Squares
            .Where(f => f.Piece != null && f.Piece.Color == fromSquare.Piece.Color && f.Piece.Type == fromSquare.Piece.Type && f.Piece.Id != fromSquare.Piece.Id)
            .ToList();

            foreach (var f in samePieces) {
                if (GetAvailableSquaresPrimitive(f, false).Contains(toSquare))
                    res.Ambiguous.Add(f);
            }

            // From now on we must do the check with the move done
            toSquare.Piece = fromSquare.Piece;
            fromSquare.Piece = null;
            var tempMove = new Move(toSquare.Piece, fromSquare, toSquare) { CapturedPiece = captured };

            // If we moved the king or the rook we cannot castle anymore
            if (toSquare.Piece.Type == Piece.Pieces.King) {
                KingCastling.Remove(ToMove);
                QueenCastling.Remove(ToMove);
            } else if (!toSquare.Piece.Moved && toSquare.Piece.Type == Piece.Pieces.Rook) {
                if (fromSquare.File < Board.InitialKingSquare[(int)ToMove])
                    QueenCastling.Remove(ToMove);
                else if (fromSquare.File > Board.InitialKingSquare[(int)ToMove])
                    KingCastling.Remove(ToMove);
            }

            toSquare.Piece.Moved = true;
            // Capture the piece
            if (captured != null) {
                CapturedPieces.Add(captured);

                // If the captured pieces is a rook remove castling
                if (captured.Type == Piece.Pieces.Rook && !captured.Moved) {
                    var tempKingSquare = Board.GetKingSquare(captured.Color);
                    if (tempKingSquare.File < toSquare.File)
                        KingCastling.Remove(captured.Color);
                    else
                        QueenCastling.Remove(captured.Color);
                }
            }

            // Record en passant position
            if (toSquare.Piece.Type == Piece.Pieces.Pawn && Math.Abs(fromSquare.Rank - toSquare.Rank) == 2) {
                if (fromSquare.Rank > toSquare.Rank)
                    EnPassant = $"{fromSquare.File}{toSquare.Rank + 1}";
                else
                    EnPassant = $"{fromSquare.File}{toSquare.Rank - 1}";
            } else {
                EnPassant = string.Empty;
            }

            // Check promotion
            if (!res.Promoted) {
                if (tempMove.Piece.Type == Piece.Pieces.Pawn) {
                    if ((tempMove.Piece.Color == Colors.White && tempMove.To.Rank == 8) ||
                        (tempMove.Piece.Color == Colors.Black && tempMove.To.Rank == 1)) {
                        if (PlayerPromotion == null || GetPlayer(tempMove.Piece.Color) is EnginePlayer)
                            tempMove.Piece.Type = Piece.Pieces.Queen;
                        else
                            tempMove.Piece.Type = await PlayerPromotion.Invoke(this, new PromotionArgs(tempMove));

                        Promoted?.Invoke(this, new PromotionArgs(tempMove));
                        res.Promoted = true;
                        move = $"{move}{tempMove.Piece.Acronym}";
                    }
                }
            }

            tempMove.CoordinateNotation = move;

            res.Move = move;
            res.Moves.Add(tempMove);

            return res;
        } // DoMoveNormal

        private void SetAlgebraicNotation(MoveNotation moveNotation, string moveStr, List<Move> moves, bool promoted, List<Board.Square> ambiguous)
        {
            if (moves.Count > 1) {
                // Castling
                if (moveStr == WhiteKingCastlingMove || moveStr == BlackKingCastlingMove)
                    moveNotation.ShortAlgebraic = "0-0";
                else
                    moveNotation.ShortAlgebraic = "0-0-0";
            } else {
                var move = moves[0];
                string aMove = string.Empty;
                string laMove = string.Empty;

                if (move.Piece.Type == Piece.Pieces.Pawn || promoted) {
                    if (move.CapturedPiece != null) {
                        aMove = $"{char.ToLower(move.From.File)}x{move.To.Notation.ToLower()}";
                        laMove = $"{move.From.Notation.ToLower()}x{move.To.Notation.ToLower()}";
                    } else {
                        aMove = move.To.Notation.ToLower();
                        laMove = $"{move.From.Notation.ToLower()}{move.To.Notation.ToLower()}";
                    }

                    if (promoted)
                        aMove = $"{aMove}{move.Piece.Acronym}";
                } else {
                    if (move.CapturedPiece != null) {
                        aMove = $"{move.Piece.Acronym}x{move.To.Notation.ToLower()}";
                        laMove = $"{move.Piece.Acronym}{move.From.Notation.ToLower()}x{move.To.Notation.ToLower()}";
                    } else {
                        aMove = $"{move.Piece.Acronym}{move.To.Notation.ToLower()}";
                        laMove = $"{move.Piece.Acronym}{move.From.Notation.ToLower()}{move.To.Notation.ToLower()}";
                    }

                    // Disambiguate moves
                    if (ambiguous?.Count > 0) {
                        bool aFile = true;
                        bool aRank = true;
                        foreach (var f in ambiguous) {
                            if (f.File == move.From.File)
                                aFile = false;
                            if (f.Rank == move.From.Rank)
                                aRank = false;
                        }

                        if (aFile && aRank)
                            aMove = aMove.Insert(1, move.From.Notation.ToLower());
                        else if (aFile)
                            aMove = aMove.Insert(1, char.ToLower(move.From.File).ToString());
                        else
                            aMove = aMove.Insert(1, move.From.Rank.ToString());
                    }
                }

                if (Result != null) {
                    if (Result == Results.Checkmate) {
                        aMove = $"{aMove}# {(Winner == Colors.White ? "1-0" : "0-1")}";
                        laMove = $"{laMove}# {(Winner == Colors.White ? "1-0" : "0-1")}";
                    } else if (Result == Results.Draw) {
                        aMove = $"{aMove} 1/2-1/2";
                        laMove = $"{laMove} 1/2-1/2";
                    }
                } else {
                    var kingSquare = Board.GetKingSquare(ToMove);
                    if (IsAttacked(kingSquare, kingSquare.Piece.Color)) {
                        aMove = $"{aMove}+";
                        laMove = $"{laMove}+";
                    }
                }
                moveNotation.ShortAlgebraic = aMove;
                moveNotation.LongAlgebraic = laMove;
            }
        } // SetAlgebraicNotation

        private static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream()) {
                using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
                    msi.CopyTo(gs);
                }
                return mso.ToArray();
            }
        } // Zip

        private static string UnZip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream()) {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress)) {
                    gs.CopyTo(mso);
                }
                return Encoding.UTF8.GetString(mso.ToArray());
            }
        } // UnZip
        #endregion
    } // Game
}
