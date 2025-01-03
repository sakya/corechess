﻿using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using ChessLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Avalonia.Threading;
using Un4seen.Bass;
using System.Threading;
using Avalonia.Reactive;
using CoreChess.Dialogs;

namespace CoreChess.Controls
{
    public partial class Chessboard : UserControl
    {
        private Game m_Game;
        private readonly double m_BorderWidth = 18.0;
        private Board.Square m_SelectedSquare;
        private DragInfo m_DragInfo;
        private string m_HighlightedMove = string.Empty;
        private bool m_GameEndedInvoked;
        private readonly Mutex m_GameEndedMutex = new();

        class DragInfo
        {
            public Piece Piece { get; set; }
            public Point StartPosition { get; set; }
            public Board.Square StartSquare { get; set; }
            public List<Board.Square> AvailableSquares { get; set; }
            public Board.Square PreviousSquare { get; set; }
            public IBrush PreviousSquareFill { get; set; }
        } // DragInfo
        #region events
        public event EventHandler NewGame;
        public event EventHandler MoveMade;

        public class EngineThinkingEventArgs : EventArgs
        {
            public EngineThinkingEventArgs(ChessLib.Engines.EngineBase engine, string message)
            {
                Engine = engine;
                Message = message;
            }

            public ChessLib.Engines.EngineBase Engine { get; set; }
            public string Message { get; set; }
        }
        public delegate void EngineThinkingHandler(object sender, EngineThinkingEventArgs e);
        public event EngineThinkingHandler EngineThinking;

        public delegate void EngineErrorHandler(object sender, string error);
        public event EngineErrorHandler EngineError;

        public class GameEndedEventArgs : EventArgs
        {
            public GameEndedEventArgs(Game.Results reason, Game.Colors? winner = null)
            {
                Reason = reason;
                Winner = winner;
            }
            public Game.Results Reason { get; set; }
            public Game.Colors? Winner { get; set; }
        }
        public delegate void GameEndedHandler(object sender, GameEndedEventArgs e);
        public event GameEndedHandler GameEnded;

        public class TimerEventArgs : EventArgs
        {
            public TimerEventArgs(int msecs)
            {
                MillisecondsLeft = msecs;
            }
            public int MillisecondsLeft { get; set; }
        }
        public delegate void TimerHandler(object sender, TimerEventArgs e);
        public event TimerHandler WhiteTimer;
        public event TimerHandler BlackTimer;
        #endregion

        public Chessboard()
        {
            InitializeComponent();

            // Subscribe to bounds changed
            var bounds = m_Canvas.GetObservable(Canvas.BoundsProperty);
            RenderOptions.SetBitmapInterpolationMode(m_Canvas, BitmapInterpolationMode.HighQuality);
            bounds.Subscribe(new AnonymousObserver<Rect>(value =>
            {
                if (m_Game != null)
                    DrawBoard(m_Canvas);
            }));

            // Default settings
            BorderColor = Colors.Transparent;
            SquareWhiteColor = Utils.ColorConverter.ParseHexColor("#ffeeeed2");
            SquareWhiteSelectedColor = Utils.ColorConverter.ParseHexColor("#fff7f783");

            SquareBlackColor = Utils.ColorConverter.ParseHexColor("#ff769656");
            SquareBlackSelectedColor = Utils.ColorConverter.ParseHexColor("#ffbbcb44");

            PiecesFolder = $"{App.PiecesPath}{System.IO.Path.DirectorySeparatorChar}Default";
            ShowAvailableMoves = true;
            ShowFileRankNotation = Settings.FileRankNotations.Inside;
        }

        public Color BorderColor { get; set; }
        public Color SquareWhiteColor { get; set; }
        public Color SquareBlackColor { get; set; }
        public Color SquareWhiteSelectedColor { get; set; }
        public Color SquareBlackSelectedColor { get; set; }
        public string PiecesFolder { get; set; }
        public bool EnableDragAndDrop { get; set; }
        public bool ShowAvailableMoves { get; set; }
        public Settings.FileRankNotations ShowFileRankNotation { get; set; }
        public bool EnableAudio { get; set; }
        public bool Flipped { get; set; }

        public double BorderWidth
        {
            get {
                if (ShowFileRankNotation == Settings.FileRankNotations.Outside)
                    return m_BorderWidth;
                return 0;
            }
        }
        public double SquareWidth
        {
            get {
                if (m_Canvas != null) {
                    return (m_Canvas.Bounds.Width - BorderWidth * 2.0) / 8.0;
                }
                return 0;
            }
        }

        public double SquareHeight
        {
            get {
                if (m_Canvas != null)
                    return (m_Canvas.Bounds.Height - BorderWidth * 2.0) / 8.0;
                return 0;
            }
        }

        public Game Game
        {
            get { return m_Game; }
        }

        #region mouse events
        private void OnMouseMoved(object sender, Avalonia.Input.PointerEventArgs args)
        {
            if (!EnableDragAndDrop) {
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
                if (!m_Game.Ended && m_Game.ToMovePlayer is HumanPlayer) {
                    if (m_SelectedSquare != null) {
                        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                    } else {
                        SetMouseCursor(args.GetPosition(m_Canvas));
                    }
                }
            } else {
                // Drag and drop mode
                Point relPos = args.GetPosition(m_Canvas);

                if (m_DragInfo != null) {
                    var piece = GetPieceGraphicElement(m_DragInfo.Piece);

                    double targetX = relPos.X - (piece.Width / 2.0);
                    if (targetX < 0)
                        targetX = 0;
                    else if (targetX + piece.Width > m_Canvas.Bounds.Width)
                        targetX = m_Canvas.Bounds.Width - piece.Width;

                    double targetY = relPos.Y - (piece.Height / 2.0);
                    if (targetY < 0)
                        targetY = 0;
                    else if (targetY + piece.Height > m_Canvas.Bounds.Height)
                        targetY = m_Canvas.Bounds.Height - piece.Height;

                    Canvas.SetLeft(piece, targetX);
                    Canvas.SetTop(piece, targetY);

                    // Highlight square
                    HighlightDragAndDropSquare(GetSquareFromPoint(new Point(targetX + (piece.Width / 2.0), targetY + (piece.Height / 2.0))));
                } else {
                    if (!m_Game.Ended && m_Game.ToMovePlayer is HumanPlayer) {
                        SetMouseCursor(relPos);
                    } else
                        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
                }
            }
        } // OnMouseMoved

        private async void OnMouseDown(object sender, Avalonia.Input.PointerPressedEventArgs args)
        {
            if (m_Game.ToMovePlayer is EnginePlayer || m_Game.Ended)
                return;

            var clickedSquare = GetSquareFromPoint(args.GetPosition(m_Canvas));
            if (clickedSquare == null)
                return;

            if (!EnableDragAndDrop) {
                if (m_SelectedSquare != null && (clickedSquare.Piece == null || clickedSquare.Piece.Color != m_SelectedSquare.Piece.Color)) {
                    await DoPlayerMove(m_SelectedSquare, clickedSquare);
                } else {
                    if (clickedSquare != m_SelectedSquare) {
                        DeselectSquare();

                        if (clickedSquare.Piece?.Color == m_Game.ToMove) {
                            // Select the square
                            var rect = GetRectangle(clickedSquare.Notation);
                            if (rect != null)
                                rect.Fill = new SolidColorBrush(clickedSquare.Color == Game.Colors.White ? SquareWhiteSelectedColor : SquareBlackSelectedColor);
                            m_SelectedSquare = clickedSquare;

                            if (ShowAvailableMoves)
                                DrawAvailableSquares(m_SelectedSquare);
                        }
                    } else {
                        DeselectSquare();
                    }
                }
            } else {
                // Drag and drop mode
                if (clickedSquare.Piece?.Color == m_Game.ToMove) {
                    var squares = m_Game.GetAvailableSquares(clickedSquare);
                    if (ShowAvailableMoves)
                        DrawAvailableSquares(clickedSquare, squares);

                    var piece = GetPieceGraphicElement(clickedSquare.Piece);
                    piece.ZIndex = 2;
                    m_DragInfo = new DragInfo()
                    {
                        Piece = clickedSquare.Piece,
                        StartPosition = new Point(Canvas.GetLeft(piece), Canvas.GetTop(piece)),
                        StartSquare = clickedSquare,
                        AvailableSquares = squares
                    };
                }
            }
        } // OnMouseDown

        private async void OnMouseUp(object sender, Avalonia.Input.PointerReleasedEventArgs args)
        {
            if (m_DragInfo != null) {
                var dragInfo = m_DragInfo;
                HighlightDragAndDropSquare(null);
                m_DragInfo = null;
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);

                if (ShowAvailableMoves)
                    RemoveAvailableSquares(dragInfo.StartSquare);

                var square = GetSquareFromPoint(args.GetPosition(m_Canvas));
                if (dragInfo.AvailableSquares.Contains(square)) {
                    if (!await DoPlayerMove(dragInfo.StartSquare, square)) {
                        // Reposition the piece
                        await AnimatePiece(dragInfo.Piece, dragInfo.StartPosition, 250, false);
                    }
                } else {
                    // Reposition the piece
                    await AnimatePiece(dragInfo.Piece, dragInfo.StartPosition, 250, false);
                }
            }
            SetMouseCursor(args.GetPosition(m_Canvas));
        } // OnMouseUp
        #endregion

        public bool SetEmpty()
        {
            m_Game = new Game();
            m_Game.Board.InitEmpty();
            DrawBoard(m_Canvas);

            return true;
        }

        public async Task<bool> SetGame(Game game)
        {
            IsHitTestVisible = false;

            m_Game = game;
            m_Game.WhiteTimer += OnGameWhiteTimer;
            m_Game.BlackTimer += OnGameBlackTimer;
            m_Game.PlayerPromotion += OnPlayerPromotion;
            m_Game.CastlingConfirm += OnCastlingConfirm;
            m_Game.Promoted += OnPromoted;
            m_Game.EngineError += OnEngineError;
            Flipped = m_Game.GameType == Game.GameTypes.HumanVsEngine && m_Game.GetPlayer(Game.Colors.Black) is HumanPlayer;
            DrawBoard(m_Canvas, lastMove: m_Game.Moves?.LastOrDefault());
            NewGame?.Invoke(this, EventArgs.Empty);
            await m_Game.Start();

            m_GameEndedInvoked = false;
            return true;
        } // SetGame

        public void StartGame()
        {
            IsHitTestVisible = true;
            if (!m_Game.Ended && m_Game.ToMovePlayer is EnginePlayer player && player.Engine != null) {
                // Don't wait this move
                _ = DoEngineMove();
            }
        } // StartGame

        public async Task<bool> UndoMove()
        {
            await m_Game.UndoLastHumanPlayerMove();
            Redraw(m_Game.Moves.Count() > 0 ? m_Game.Moves.Last() : null);
            return true;
        } // UndoMove

        public async Task<bool> ResignGame()
        {
            if (!m_Game.Ended) {
                await m_Game.Resign();
                InvokeGameEnded();
            }
            return true;
        } // ResignGame

        public void Redraw(Game.MoveNotation lastMove = null)
        {
            DrawBoard(m_Canvas, lastMove: lastMove);
        } // Redraw

        public RenderTargetBitmap GetBitmap(Size size, Game.MoveNotation lastMove = null)
        {
            Canvas tCanvas = new Canvas();
            RenderOptions.SetBitmapInterpolationMode(tCanvas, BitmapInterpolationMode.HighQuality);
            DrawBoard(tCanvas, size.Width, size.Height, lastMove);
            tCanvas.Arrange(new Rect(size));

            var pixelSize = new PixelSize((int)tCanvas.Bounds.Width, (int)tCanvas.Bounds.Height);
            var renderBitmap = new RenderTargetBitmap(pixelSize);
            renderBitmap.Render(tCanvas);
            return renderBitmap;
        } // GetBitmap

        public void SaveToPng(string fileName, Size? size = null)
        {
            if (size == null)
                size = new Size(800, 800);

            using (var renderBitmap = GetBitmap(size.Value)) {
                renderBitmap.Save(fileName);
            }
        } // SaveToPng

        #region private operations
        private Canvas GetPieceGraphicElement(Piece piece)
        {
            return m_Canvas.Children
                .FirstOrDefault(c => c.Name == $"Piece_{piece.Id}") as Canvas;
        } // GetPieceGraphicElement

        private async Task<bool> CancelDragAndDrop()
        {
            if (m_DragInfo != null) {
                if (ShowAvailableMoves)
                    RemoveAvailableSquares(m_DragInfo.StartSquare);

                var dragInfo = m_DragInfo;
                HighlightDragAndDropSquare(null);
                m_DragInfo = null;
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);

                // Reposition the piece
                await AnimatePiece(dragInfo.Piece, dragInfo.StartPosition, 250, false);
            }
            return true;
        } // CancelDragAndDrop

        private async Task<Piece.Pieces> OnPlayerPromotion(object sender, Game.PromotionArgs args)
        {
            var dlg = new PromotionDialog(PiecesFolder, args.Player.Color);
            return await dlg.Show<Piece.Pieces>((Window)VisualRoot);
        } // OnPromotion

        private async Task<bool> OnCastlingConfirm(object sender, EventArgs args)
        {
            return await MessageDialog.ShowConfirmMessage((Window)VisualRoot, Localizer.Localizer.Instance["Confirm"], Localizer.Localizer.Instance["ConfirmCastling"]);
        } // OnCastlingConfirm

        private bool OnPromoted(object sender, Game.PromotionArgs args)
        {
            var piece = GetPieceGraphicElement(args.Move.Piece);
            piece.DataContext = args.Move.Piece.Type;
            piece.Background = GetBrush(GetPieceImagePath(args.Move.Piece));

            return true;
        } // OnPromoted

        private void OnEngineError(object sender, string error)
        {
            EngineError?.Invoke(this, error);
        } // OnEngineError

        private async void OnGameBlackTimer(object sender, EventArgs e)
        {
            BlackTimer?.Invoke(this, new TimerEventArgs(m_Game.BlackTimeLeftMilliSecs ?? 0));
            if (m_Game.Result != null) {
                await Dispatcher.UIThread.InvokeAsync(() => CancelDragAndDrop());
                InvokeGameEnded();
            }
        } // OnGameBlackTimer

        private async void OnGameWhiteTimer(object sender, EventArgs e)
        {
            WhiteTimer?.Invoke(this, new TimerEventArgs(m_Game.WhiteTimeLeftMilliSecs ?? 0));
            if (m_Game.Result != null) {
                await Dispatcher.UIThread.InvokeAsync(() => CancelDragAndDrop());
                InvokeGameEnded();
            }
        } // OnGameWhiteTimer

        private async Task<bool> DoPlayerMove(Board.Square from, Board.Square to)
        {
            // Try to move
            try {
                string playerMove = $"{from.Notation}{to.Notation}".ToLower();
                var movedPieces = await m_Game.DoHumanPlayerMove(playerMove);
                DeselectSquare();
                HighlightMove(m_Game.Moves.Last());

                bool first = true;
                foreach (var mp in movedPieces) {
                    var tempCanvas = await AnimatePiece(mp.Piece, GetSquarePosition(mp.To), first && EnableDragAndDrop ? 100 : 500, true, mp.CapturedPiece == null);
                    if (mp.CapturedPiece != null && tempCanvas != null) {
                        var cp = GetPieceGraphicElement(mp.CapturedPiece);
                        m_Canvas.Children.Remove(cp);
                        tempCanvas.ZIndex = 1;
                    }
                    first = false;
                }

                MoveMade?.Invoke(this, EventArgs.Empty);

                if (m_Game.Result != null) {
                    InvokeGameEnded();
                    return true;
                }

                // Engine move
                if (m_Game.ToMovePlayer is EnginePlayer)
                    await DoEngineMove();
            } catch (Exception ex) {
                Debug.WriteLine($"Exception: {ex.Message}");
                return false;
            }

            return true;
        } // DoPlayerMove

        private async Task<bool> DoEngineMove()
        {
            IsHitTestVisible = false;

            List<Game.Move> movedPieces = await m_Game.DoEnginePlayerMove((engine, output) => { EngineThinking?.Invoke(this, new EngineThinkingEventArgs(engine, output)); });
            if (movedPieces == null) {
                IsHitTestVisible = true;
                return false;
            }

            HighlightMove(m_Game.Moves.Last());
            foreach (var mp in movedPieces) {
                var tempCanvas = await AnimatePiece(mp.Piece, GetSquarePosition(mp.To), resetZindex: mp.CapturedPiece == null);
                if (mp.CapturedPiece != null && tempCanvas != null) {
                    var cp = GetPieceGraphicElement(mp.CapturedPiece);
                    m_Canvas.Children.Remove(cp);
                    tempCanvas.ZIndex = 1;
                }
            }

            // Promotion
            if (movedPieces.Count == 1) {
                var piece = GetPieceGraphicElement(movedPieces[0].Piece);
                if (piece != null && (Piece.Pieces)piece.DataContext != movedPieces[0].Piece.Type) {
                    piece.DataContext = movedPieces[0].Piece.Type;
                    piece.Background = GetBrush(GetPieceImagePath(movedPieces[0].Piece));
                }
            }
            IsHitTestVisible = true;

            MoveMade?.Invoke(this, EventArgs.Empty);
            if (m_Game.Result != null)
                InvokeGameEnded();
            else if (m_Game.ToMovePlayer is EnginePlayer player && player.Engine != null) {
                // Don't wait this move
                _ = DoEngineMove();
            }
            return true;
        } // DoEngineMove

        private void DeselectSquare()
        {
            if (m_SelectedSquare != null) {
                var rect = GetRectangle(m_SelectedSquare.Notation);
                if (rect != null) {
                    rect.Fill = new SolidColorBrush(m_SelectedSquare.Color == Game.Colors.White ? SquareWhiteColor : SquareBlackColor);
                    rect.Stroke = null;
                    rect.StrokeThickness = 0;
                }

                if (ShowAvailableMoves)
                    RemoveAvailableSquares();
                m_SelectedSquare = null;
            }
        } // DeselectSquare

        private Point GetSquarePosition(Board.Square square)
        {
            int rank = square.Rank;
            int file = m_Game.Board.Files.IndexOf(square.File);

            if (Flipped)
                return new Point(SquareWidth * (7 - file) + BorderWidth, SquareHeight * (rank - 1) + BorderWidth);
            return new Point(SquareWidth * file + BorderWidth, m_Canvas.Bounds.Height - (SquareHeight * rank) - BorderWidth);
        } // GetSquarePosition

        private Board.Square GetSquareFromPoint(Point pos)
        {
            int file = Flipped ? 7 - ((int)(pos.X - BorderWidth) / (int)SquareWidth) : (int)(pos.X - BorderWidth) / (int)SquareWidth;
            int rank = Flipped ? 7 - ((int)(pos.Y - BorderWidth) / (int)SquareHeight) : (int)(pos.Y - BorderWidth) / (int)SquareHeight;
            if (file > 7 || rank > 7 || file < 0 || rank < 0)
                return null;

            return m_Game.Board.GetSquare($"{m_Game.Board.Files[file]}{8 - rank}");
        } // GetSquareFromPoint

        private void DrawBoard(Canvas canvas, double targetWidth = 0, double targetHeight = 0, Game.MoveNotation lastMove = null)
        {
            canvas.Children.Clear();

            canvas.Background = new SolidColorBrush(BorderColor);

            double width = targetWidth == 0 ? canvas.Bounds.Width : targetWidth;
            double height = targetHeight == 0 ? canvas.Bounds.Height : targetHeight;
            if (width == 0)
                return;

            double squareWidth = (width - BorderWidth * 2.0) / 8.0;
            double squareHeight = (height - BorderWidth * 2.0) / 8.0;

            double top = BorderWidth;
            double left = BorderWidth;

            List<Color> colors = new List<Color>() { SquareWhiteColor, SquareBlackColor };
            int colorIndex = 0;

            int file = Flipped ? 7 : 0;
            int rank = Flipped ? 7 : 0;
            for (int i = 0; i < 64; i++) {
                var square = m_Game.Board.GetSquare($"{m_Game.Board.Files[file]}{8 - rank}");
                var radius = i switch
                {
                    0 => new CornerRadius(3, 0, 0, 0),
                    7 => new CornerRadius(0, 3, 0, 0),
                    56 => new CornerRadius(0, 0, 0, 3),
                    63 => new CornerRadius(0, 0, 3, 0),
                    _ => new CornerRadius(0)
                };
                var bkg = new Border()
                {
                    Name = $"Border_{square.Notation}",
                    CornerRadius = radius,
                    ClipToBounds = true,
                    ZIndex = 0,
                    Child = new Rectangle()
                    {
                        Name = $"Rect_{square.Notation}",
                        Width = squareWidth,
                        Height = squareHeight,
                        Fill = new SolidColorBrush(colors[colorIndex]),
                    }
                };
                Canvas.SetLeft(bkg, left);
                Canvas.SetTop(bkg, top);
                canvas.Children.Add(bkg);

                if (ShowFileRankNotation != Settings.FileRankNotations.None) {
                    if ((!Flipped && rank == 7) || (Flipped && rank == 0)) {
                        TextBlock text = new TextBlock()
                        {
                            TextAlignment = TextAlignment.Right,
                            Text = char.ToLower(m_Game.Board.Files[file]).ToString(),
                            FontWeight = FontWeight.Medium,
                            FontSize = 14
                        };

                        if (ShowFileRankNotation == Settings.FileRankNotations.Inside) {
                            text.Foreground = colorIndex == 0 ? new SolidColorBrush(colors[1]) : new SolidColorBrush(colors[0]);
                            Canvas.SetLeft(text, left + squareWidth - 10);
                            Canvas.SetTop(text, top + squareHeight - 20);
                            canvas.Children.Add(text);
                        } else {
                            text.Text = text.Text.ToUpper();
                            text.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                            text.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;

                            var border = new Border()
                            {
                                Height = BorderWidth,
                                Width = squareWidth
                            };
                            border.Child = text;
                            Canvas.SetLeft(border, left);
                            Canvas.SetTop(border, height - BorderWidth);
                            canvas.Children.Add(border);
                        }
                    }

                    if ((!Flipped && file == 0) || (Flipped && file == 7)) {
                        TextBlock text = new TextBlock()
                        {
                            TextAlignment = TextAlignment.Left,
                            Text = (8 - rank).ToString(),
                            FontWeight = FontWeight.Medium,
                            FontSize = 14
                        };

                        if (ShowFileRankNotation == Settings.FileRankNotations.Inside) {
                            text.Foreground = colorIndex == 0 ? new SolidColorBrush(colors[1]) : new SolidColorBrush(colors[0]);
                            Canvas.SetLeft(text, 5);
                            Canvas.SetTop(text, top + 5);
                            canvas.Children.Add(text);
                        } else {
                            text.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                            text.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;

                            var border = new Border()
                            {
                                Height = squareHeight,
                                Width = BorderWidth
                            };
                            border.Child = text;
                            Canvas.SetLeft(border, 0);
                            Canvas.SetTop(border, top);
                            canvas.Children.Add(border);
                        }
                    }
                }

                // Draw the piece
                if (square.Piece != null) {
                    var pCanvas = new Canvas()
                    {
                        Name = $"Piece_{square.Piece.Id}",
                        Width = squareWidth,
                        Height = squareHeight,
                        Background = GetBrush(GetPieceImagePath(square.Piece)),
                        DataContext = square.Piece.Type,
                        ZIndex = 1
                    };
                    RenderOptions.SetBitmapInterpolationMode(pCanvas, BitmapInterpolationMode.HighQuality);

                    Canvas.SetLeft(pCanvas, left);
                    Canvas.SetTop(pCanvas, top);
                    canvas.Children.Add(pCanvas);
                }

                left += squareWidth;
                if (Flipped) {
                    if (file == 0) {
                        left = BorderWidth;
                        top += squareHeight;
                        file = 7;
                        rank--;
                    } else {
                        file--;
                        colorIndex = colorIndex == 0 ? 1 : 0;
                    }
                } else {
                    if (file == 7) {
                        left = BorderWidth;
                        top += squareHeight;
                        file = 0;
                        rank++;
                    } else {
                        file++;
                        colorIndex = colorIndex == 0 ? 1 : 0;
                    }
                }
            }
            HighlightMove(lastMove);
        } // DrawBoard

        private IBrush GetBrush(string path)
        {
            var bitmap = new Bitmap(path);
            var image = new ImageDrawing
            {
                ImageSource = bitmap,
                Rect = new Rect(bitmap.Size)
            };
            return new DrawingBrush(image);
        }

        private void HighlightDragAndDropSquare(Board.Square square)
        {
            Rectangle rect;
            if (m_DragInfo.PreviousSquare != null) {
                rect = GetRectangle(m_DragInfo.PreviousSquare.Notation);
                if (rect != null)
                    rect.Fill = m_DragInfo.PreviousSquareFill;
            }

            if (square != null) {
                rect = GetRectangle(square.Notation);
                if (rect != null) {
                    m_DragInfo.PreviousSquare = square;
                    m_DragInfo.PreviousSquareFill = rect.Fill;
                    rect.Fill = new SolidColorBrush(square.Color == Game.Colors.White ? SquareWhiteSelectedColor : SquareBlackSelectedColor);
                }
            }
        } // HighlightDragAndDropSquare

        private void HighlightMove(Game.MoveNotation move)
        {
            if (move == null && m_Game.Moves.Count == 0)
                return;

            List<Board.Square> squares;
            if (!string.IsNullOrEmpty(m_HighlightedMove)) {
                squares = new List<Board.Square>()
                {
                    m_Game.Board.GetSquare(m_HighlightedMove.Substring(0, 2)),
                    m_Game.Board.GetSquare(m_HighlightedMove.Substring(2, 2))
                };

                foreach (var square in squares) {
                    var rect = GetRectangle(square.Notation);
                    if (rect != null)
                        rect.Fill = new SolidColorBrush(square.Color == Game.Colors.White ? SquareWhiteColor : SquareBlackColor);
                }
            }

            m_HighlightedMove = string.Empty;
            if (move != null) {
                m_HighlightedMove = move.Coordinate;
                squares = new List<Board.Square>()
                {
                    m_Game.Board.GetSquare(m_HighlightedMove.Substring(0, 2)),
                    m_Game.Board.GetSquare(m_HighlightedMove.Substring(2, 2))
                };

                foreach (var square in squares) {
                    var rect = GetRectangle(square.Notation);
                    if (rect != null)
                        rect.Fill = new SolidColorBrush(square.Color == Game.Colors.White ? SquareWhiteSelectedColor : SquareBlackSelectedColor);
                }
            }
        } // HighlightMove

        /// <summary>
        /// Draw available squares marks
        /// </summary>
        /// <param name="square">The starting <see cref="Board.Square"/></param>
        /// <returns></returns>
        private List<Board.Square> DrawAvailableSquares(Board.Square square, List<Board.Square> squares = null)
        {
            if (squares == null)
                squares = m_Game.GetAvailableSquares(square);

            if (EnableDragAndDrop) {
                var rect = GetRectangle(square.Notation);
                if (rect != null)
                    rect.Fill = new SolidColorBrush(square.Color == Game.Colors.White ? SquareWhiteSelectedColor : SquareBlackSelectedColor);
            }

            var created = new HashSet<string>();
            foreach (var s in squares) {
                // Avoid adding the same circle multiple times (this happens in Chess960 for castling)
                if (created.Contains(s.Notation))
                    continue;

                var fRect = GetRectangle(s.Notation);
                if (fRect != null) {
                    var size = fRect.Width / 4;
                    if (s.Piece != null && s != square)
                        size = fRect.Width;
                    var circle = new Ellipse()
                    {
                        Name = $"Circle_{s.Notation}",
                        Width = size,
                        Height = size,
                        Fill = new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)),
                        ZIndex = 0
                    };
                    Canvas.SetLeft(circle, Canvas.GetLeft(fRect.Parent!) + (fRect.Width / 2) - (size / 2));
                    Canvas.SetTop(circle, Canvas.GetTop(fRect.Parent) + (fRect.Height / 2) - (size / 2));
                    m_Canvas.Children.Add(circle);

                    created.Add(s.Notation);
                }
            }
            return squares;
        } // DrawAvailableSquares

        /// <summary>
        /// Remove all available squares marks
        /// </summary>
        /// <param name="square">The starting <see cref="Board.Square"/></param>
        private void RemoveAvailableSquares(Board.Square square = null)
        {
            if (square != null) {
                var rect = GetRectangle(square.Notation);
                if (rect != null)
                    rect.Fill = new SolidColorBrush(square.Color == Game.Colors.White ? SquareWhiteColor : SquareBlackColor);
            }
            m_Canvas.Children.RemoveAll(m_Canvas.Children.Where(c => c.Name != null && c.Name.StartsWith("Circle_")).ToList());
        } // RemoveAvailableSquares

        private async Task<Canvas> AnimatePiece(Piece piece, Point targetPosition, int durationMs = 500, bool playAudio = true, bool resetZindex = true)
        {
            var pGraphics = GetPieceGraphicElement(piece);
            if (pGraphics == null)
                return null;

            pGraphics.ZIndex = 2;
            Animation anim = new Animation()
            {
                Duration = TimeSpan.FromMilliseconds(durationMs),
                Easing = new Avalonia.Animation.Easings.LinearEasing(),
                FillMode = FillMode.Forward
            };

            var kf = new KeyFrame()
            {
                Cue = new Cue(0.0)
            };
            kf.Setters.Add(new Setter()
            {
                Property = Canvas.LeftProperty,
                Value = Canvas.GetLeft(pGraphics)
            });
            anim.Children.Add(kf);

            kf = new KeyFrame()
            {
                Cue = new Cue(1.0)
            };
            kf.Setters.Add(new Setter()
            {
                Property = Canvas.LeftProperty,
                Value = targetPosition.X
            });
            anim.Children.Add(kf);

            kf = new KeyFrame()
            {
                Cue = new Cue(0.0)
            };
            kf.Setters.Add(new Setter()
            {
                Property = Canvas.TopProperty,
                Value = Canvas.GetTop(pGraphics)
            });
            anim.Children.Add(kf);

            kf = new KeyFrame()
            {
                Cue = new Cue(1.0)
            };
            kf.Setters.Add(new Setter()
            {
                Property = Canvas.TopProperty,
                Value = targetPosition.Y
            });
            anim.Children.Add(kf);

            int audioStream = 0;
            if (EnableAudio && playAudio) {
                audioStream = piece.Color == Game.Colors.White ?
                    Bass.BASS_StreamCreateFile("Audio/move_white.mp3", 0, 0, BASSFlag.BASS_DEFAULT) :
                    Bass.BASS_StreamCreateFile("Audio/move_black.mp3", 0, 0, BASSFlag.BASS_DEFAULT);
                Bass.BASS_ChannelPlay(audioStream, false);
            }
            await anim.RunAsync(pGraphics, CancellationToken.None);

            Canvas.SetTop(pGraphics, targetPosition.Y);
            Canvas.SetLeft(pGraphics, targetPosition.X);

            if (resetZindex)
                pGraphics.ZIndex = 1;

            while (audioStream != 0 && Bass.BASS_ChannelIsActive(audioStream) == BASSActive.BASS_ACTIVE_PLAYING)
                await Task.Delay(10);
            Bass.BASS_StreamFree(audioStream);

            return pGraphics;
        } // AnimatePiece

        private string GetPieceImagePath(Piece piece)
        {
            if (piece.Color == Game.Colors.White)
                return $"{PiecesFolder}{System.IO.Path.DirectorySeparatorChar}w{piece.Type.ToString()}.png";
            else
                return $"{PiecesFolder}{System.IO.Path.DirectorySeparatorChar}b{piece.Type.ToString()}.png";
        } // GetPieceImagePath

        private void InvokeGameEnded()
        {
            m_GameEndedMutex.WaitOne();
            if (!m_GameEndedInvoked) {
                GameEnded?.Invoke(this, new GameEndedEventArgs(m_Game.Result.Value, m_Game.Winner));
                m_GameEndedInvoked = true;
            }
            m_GameEndedMutex.ReleaseMutex();
        } // InvokeGameEnded

        private void SetMouseCursor(Point point)
        {
            var overSquare = GetSquareFromPoint(point);
            if (overSquare?.Piece != null && overSquare.Piece.Color == m_Game.ToMovePlayer.Color)
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
            else
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        } // SetMouseCursor

        private Rectangle GetRectangle(string notation)
        {
            if (m_Canvas.Children.FirstOrDefault(c => c.Name == $"Border_{notation}") is Border border) {
                return border.Child as Rectangle;
            }
            return null;
        }
        #endregion
    }
}
