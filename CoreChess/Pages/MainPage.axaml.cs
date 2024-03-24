using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChessLib;
using ChessLib.Engines;
using CoreChess.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using CoreChess.Abstracts;
using CoreChess.Dialogs;

namespace CoreChess.Pages
{
    public partial class MainPage : BasePage
    {
        #region classes
        private class Context : INotifyPropertyChanged
        {
            #region commands
            private class MoveNotationCommand : ICommand
            {
                Context m_Owner;
                public event EventHandler CanExecuteChanged
                {
                    add { }
                    remove { }
                }

                public MoveNotationCommand(Context owner)
                {
                    m_Owner = owner;
                }
                public bool CanExecute(object parameter)
                {
                    return true;
                }

                public async void Execute(object parameter)
                {
                    Settings.Notations notation;
                    if (Enum.TryParse((string)parameter, out notation)) {
                        App.Settings.MoveNotation = notation;
                        App.Settings.Save(App.SettingsPath);
                        m_Owner.MoveNotation = notation;
                        await m_Owner.Page.UpdateMoves();
                    }
                }
            }

            private class CapturedPiecesCommand : ICommand
            {
                Context m_Owner;
                public event EventHandler CanExecuteChanged
                {
                    add { }
                    remove { }
                }

                public CapturedPiecesCommand(Context owner)
                {
                    m_Owner = owner;
                }
                public bool CanExecute(object parameter)
                {
                    return true;
                }

                public void Execute(object parameter)
                {
                    Settings.CapturedPiecesDisplay setting;
                    if (Enum.TryParse((string)parameter, out setting)) {
                        App.Settings.CapturedPieces = setting;
                        App.Settings.Save(App.SettingsPath);
                        m_Owner.CapturedPieces = setting;
                        m_Owner.Page.UpdateCapturedPieces();
                    }
                }
            }

            private class ShowEngineOutputCommand : ICommand
            {
                Context m_Owner;
                public event EventHandler CanExecuteChanged
                {
                    add { }
                    remove { }
                }

                public ShowEngineOutputCommand(Context owner)
                {
                    m_Owner = owner;
                }
                public bool CanExecute(object parameter)
                {
                    return true;
                }

                public void Execute(object parameter)
                {
                    App.Settings.ShowEngineOutput = !App.Settings.ShowEngineOutput;
                    App.Settings.Save(App.SettingsPath);
                    m_Owner.ShowEngineOutput = App.Settings.ShowEngineOutput;

                    if (!m_Owner.Page.m_GameAnalyzeSection.IsVisible)
                        m_Owner.Page.m_EngineMessageSection.IsVisible = App.Settings.ShowEngineOutput;
                }
            }

            private class ZenModeCommand : ICommand
            {
                Context m_Owner;
                public event EventHandler CanExecuteChanged
                {
                    add { }
                    remove { }
                }

                public ZenModeCommand(Context owner)
                {
                    m_Owner = owner;
                }
                public bool CanExecute(object parameter)
                {
                    return true;
                }

                public void Execute(object parameter)
                {
                    m_Owner.ZenMode = !m_Owner.ZenMode;

                    if (m_Owner.ZenMode) {
                        m_Owner.Page.MinWidth = 0;
                        App.MainWindow.SaveWindowSizeAndPosition();
                        m_Owner.Page.m_SidePanel.IsVisible = false;
                        m_Owner.Page.m_Menu.IsVisible = false;
                        if (App.MainWindow.WindowState == WindowState.Maximized)
                            m_Owner.ContentAlignment = "Center";
                        else {
                            m_Owner.ContentAlignment = "Stretch";
                            App.MainWindow.UpdateLayout();
                            App.MainWindow.Width = m_Owner.Page.m_Chessboard.Width + m_Owner.Page.m_Content.Margin.Left + m_Owner.Page.m_Content.Margin.Right;
                            App.MainWindow.MaxWidth = App.MainWindow.Width;
                        }
                        App.MainWindow.CanResize = false;
                    } else {
                        App.MainWindow.MinWidth = 600;
                        App.MainWindow.MaxWidth = double.PositiveInfinity;
                        m_Owner.Page.m_SidePanel.IsVisible = true;
                        m_Owner.Page.m_Menu.IsVisible = true;
                        App.MainWindow.RestoreWindowSizeAndPosition();
                        m_Owner.ContentAlignment = "Stretch";
                        App.MainWindow.CanResize = true;
                    }
                }
            }
            #endregion

            private bool m_IsResignEnabled;
            private bool m_isEngineSettingsEnabled;
            private bool m_CanPause;
            private bool m_IsPaused;
            private bool m_IsWindows;
            private bool m_CheckingForUpdates;
            private Settings.Notations? m_MoveNotation;
            private Settings.CapturedPiecesDisplay? m_CapturedPieces;
            private bool m_ShowEngineOutput;
            private bool m_ZenMode;
            private string m_ContentAlignment = "Stretch";
            private string m_WhiteName = string.Empty;
            private int? m_WhiteElo;
            private string m_BlackName = string.Empty;
            private int? m_BlackElo;
            private string m_WhiteTime = string.Empty;
            private string m_BlackTime = string.Empty;
            private string m_EcoName = string.Empty;

            public event PropertyChangedEventHandler PropertyChanged;

            public Context(MainPage window)
            {
                Page = window;
                IsResignEnabled = false;
                CanPause = false;
                OnMoveNotationClick = new MoveNotationCommand(this);
                OnCapturedPiecesClick = new CapturedPiecesCommand(this);
                OnShowEngineOutputClick = new ShowEngineOutputCommand(this);
                OnZenModeClick = new ZenModeCommand(this);
            }

            public MainPage Page { get; private set; }

            public bool IsWindows
            {
                get { return m_IsWindows; }
                set { SetIfChanged(ref m_IsWindows, value); }
            }

            public bool CheckingForUpdates
            {
                get { return m_CheckingForUpdates; }
                set { SetIfChanged(ref m_CheckingForUpdates, value); }
            }

            public bool IsPaused
            {
                get { return m_IsPaused; }
                set { SetIfChanged(ref m_IsPaused, value); }
            }

            public bool CanPause
            {
                get { return m_CanPause; }
                set { SetIfChanged(ref m_CanPause, value); }
            }

            public bool IsResignEnabled
            {
                get { return m_IsResignEnabled; }
                set { SetIfChanged(ref m_IsResignEnabled, value); }
            }

            public bool IsEngineSettingsEnabled
            {
                get { return m_isEngineSettingsEnabled; }
                set { SetIfChanged(ref m_isEngineSettingsEnabled, value); }
            }

            public Settings.Notations? MoveNotation
            {
                get { return m_MoveNotation; }
                set { SetIfChanged(ref m_MoveNotation, value); }
            }

            public Settings.CapturedPiecesDisplay? CapturedPieces
            {
                get { return m_CapturedPieces; }
                set { SetIfChanged(ref m_CapturedPieces, value); }
            }

            public bool ShowEngineOutput
            {
                get { return m_ShowEngineOutput; }
                set { SetIfChanged(ref m_ShowEngineOutput, value); }
            }

            public bool ZenMode
            {
                get { return m_ZenMode; }
                set { SetIfChanged(ref m_ZenMode, value); }
            }

            public string ContentAlignment
            {
                get { return m_ContentAlignment; }
                set { SetIfChanged(ref m_ContentAlignment, value); }
            }

            public string WhiteName
            {
                get { return m_WhiteName; }
                set { SetIfChanged(ref m_WhiteName, value); }
            }

            public int? WhiteElo
            {
                get { return m_WhiteElo; }
                set { SetIfChanged(ref m_WhiteElo, value); }
            }

            public string WhiteTime
            {
                get { return m_WhiteTime; }
                set { SetIfChanged(ref m_WhiteTime, value); }
            }

            public string BlackName
            {
                get { return m_BlackName; }
                set { SetIfChanged(ref m_BlackName, value); }
            }

            public int? BlackElo
            {
                get { return m_BlackElo; }
                set { SetIfChanged(ref m_BlackElo, value); }
            }

            public string BlackTime
            {
                get { return m_BlackTime; }
                set { SetIfChanged(ref m_BlackTime, value); }
            }

            public string EcoName
            {
                get { return m_EcoName; }
                set { SetIfChanged(ref m_EcoName, value); }
            }

            public ICommand OnMoveNotationClick { get; set; }
            public ICommand OnCapturedPiecesClick { get; set; }
            public ICommand OnShowEngineOutputClick { get; set; }
            public ICommand OnZenModeClick { get; set; }

            private void SetIfChanged<T>(ref T target, T value, [CallerMemberName] string propertyName = "")
            {
                if (propertyName == null)
                    throw new ArgumentNullException(nameof(propertyName));

                if (!EqualityComparer<T>.Default.Equals(target, value)) {
                    target = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        } // Context
        #endregion

        private readonly string[] m_Args;
        private bool m_Initialized;
        private readonly Context m_Context;
        private Game m_Game;
        private readonly List<string> m_EngineMessagesRows = new();
        private readonly Utils.EcoDatabase m_EcoDatabase;
        private int? m_CurrentMoveIndex;
        private List<Piece.Pieces> m_LastWhiteCapturedPieces = new();
        private List<Piece.Pieces> m_LastBlackCapturedPieces = new();

        public MainPage()
        {
            InitializeComponent();
        }

        public MainPage(string[] args)
        {
            m_Args = args;
            InitializeComponent();

            m_EcoDatabase = App.EcoDatabase;

            SetMru();
            m_Context = new Context(this);
            m_Context.IsWindows = OperatingSystem.IsWindows();
            m_Context.MoveNotation = App.Settings.MoveNotation;
            m_Context.CapturedPieces = App.Settings.CapturedPieces;
            this.DataContext = m_Context;

            m_Wait.AttachedToVisualTree += InitializeWindow;

            App.MainWindow.Closing += OnWindowClosing;
        }

        #region Chessboard events
        public async void OnNewGame(object sender, EventArgs e)
        {
            UpdateCapturedPieces();
            await UpdateMoves();
            SetPlayerToMove();
        } // OnNewGame

        public void OnWhiteTimer(object sender, Chessboard.TimerEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (m_Game == null)
                    return;

                TimeSpan ts;
                if (m_Game.WhiteTimeLeftMilliSecs != null)
                    ts = TimeSpan.FromMilliseconds(e.MillisecondsLeft);
                else {
                    ts = TimeSpan.FromMilliseconds(m_Game.WhiteTimeMilliSecs);
                }

                if (ts.TotalHours > 1 || ts.TotalHours < -1)
                    m_Context.WhiteTime = $"{(ts < TimeSpan.Zero ? "-" : "")}{ts.ToString(@"hh\:mm\:ss")}";
                else
                    m_Context.WhiteTime = $"{(ts < TimeSpan.Zero ? "-" : "")}{ts.ToString(@"mm\:ss")}";
            });
        } // OnWhiteTimer

        public void OnBlackTimer(object sender, Chessboard.TimerEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (m_Game == null)
                    return;

                TimeSpan ts;
                if (m_Game.WhiteTimeLeftMilliSecs != null)
                    ts = TimeSpan.FromMilliseconds(e.MillisecondsLeft);
                else {
                    ts = TimeSpan.FromMilliseconds(m_Game.BlackTimeMilliSecs);
                }

                if (ts.TotalHours > 1 || ts.TotalHours < -1)
                    m_Context.BlackTime = $"{(ts < TimeSpan.Zero ? "-" : "")}{ts.ToString(@"hh\:mm\:ss")}";
                else
                    m_Context.BlackTime = $"{(ts < TimeSpan.Zero ? "-" : "")}{ts.ToString(@"mm\:ss")}";
            });
        } // OnBlackTimer

        public async void OnEngineError(object sender, string error)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"], error, MessageDialog.Icons.Error);
            });
        } // OnEngineError

        public void OnEngineThinking(object sender, Chessboard.EngineThinkingEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Message)) {
                var info = args.Engine.ParseThinkingInfo(args.Message);
                if (info != null) {
                    StringBuilder sb = new StringBuilder();
                    if (info.Depth.HasValue)
                        sb.Append($"Depth {info.Depth.Value.ToString(App.Settings.Culture)}");
                    if (info.SelDepth.HasValue)
                        sb.Append($" seldepth {info.SelDepth.Value.ToString(App.Settings.Culture)}");

                    if (info.Score.MateIn != 0)
                        sb.Append($" mate in {(info.Score.MateIn).ToString(App.Settings.Culture)}");
                    else
                        sb.Append($" score {(info.Score.CentiPawns / 100.0).ToString("+0.##;-0.##", App.Settings.Culture)}");

                    if (info.Nodes.HasValue)
                        sb.Append($" nodes {info.Nodes.Value.ToString(App.Settings.Culture)}");
                    if (info.NodesPerSecond.HasValue)
                        sb.Append($" kn/s {(info.NodesPerSecond.Value / 1000).ToString(App.Settings.Culture)}");
                    if (info.Time.HasValue)
                        sb.Append($" time {info.Time.Value.ToString(App.Settings.Culture)}");

                    if (m_EngineMessagesRows.Count >= 5)
                        m_EngineMessagesRows.RemoveAt(m_EngineMessagesRows.Count - 1);
                    m_EngineMessagesRows.Insert(0, sb.ToString());
                    m_EngineMessage.Text = string.Join(Environment.NewLine, m_EngineMessagesRows);
                }
            }
        } // OnEngineThinking

        public async void OnMoveMade(object sender, EventArgs e)
        {
            m_Context.IsResignEnabled = CanResignOrPause();
            m_Context.CanPause = CanResignOrPause();

            UpdateCapturedPieces();

            using (var game = new Game()) {
                var settings = new Game.GameSettings();
                settings.InitialFenPosition = m_Game.Settings.InitialFenPosition;
                settings.IsChess960 = m_Game.Settings.IsChess960;
                settings.Players.Add(new HumanPlayer(Game.Colors.White, string.Empty, null));
                settings.Players.Add(new HumanPlayer(Game.Colors.Black, string.Empty, null));
                game.Init(settings);

                var chessboard = new Chessboard();
                chessboard.PiecesFolder = App.GetPiecesPath(App.Settings.PiecesSet);
                chessboard.SquareWhiteColor = Utils.ColorConverter.ParseHexColor(App.Settings.WhiteColor);
                chessboard.SquareWhiteSelectedColor = Utils.ColorConverter.ParseHexColor(App.Settings.WhiteSelectedColor);
                chessboard.SquareBlackColor = Utils.ColorConverter.ParseHexColor(App.Settings.BlackColor);
                chessboard.SquareBlackSelectedColor = Utils.ColorConverter.ParseHexColor(App.Settings.BlackSelectedColor);
                chessboard.ShowFileRankNotation = Settings.FileRankNotations.None;
                await chessboard.SetGame(game);
                chessboard.Flipped = m_Game.GetPlayer(Game.Colors.Black) is HumanPlayer && m_Game.GetPlayer(Game.Colors.White) is EnginePlayer;

                foreach (var m in m_Game.Moves) {
                    await game.DoMove(m.Coordinate, false, true);
                }

                if (game.Moves.Count > 0) {
                    var stack = AddMove(chessboard, m_Game.Moves.Last());
                    DispatcherTimer.RunOnce(() =>
                    {
                        stack.BringIntoView();
                    }, TimeSpan.FromMilliseconds(10), DispatcherPriority.Background);
                    if (!m_Game.Settings.IsChess960)
                        UpdateEco();
                }
            }
            SetPlayerToMove();
        } // OnMoveMade

        public async void OnGameEnded(object sender, Chessboard.GameEndedEventArgs e)
        {
            // Save the game in the database
            if (m_Game.FullMoveNumber > 0)
                await m_Game.Save(Path.Join(App.GamesDatabasePath, $"{Guid.NewGuid().ToString("N")}.ccsf"));

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                OnResumeClick(null, new RoutedEventArgs());
                MessageDialog.CloseOpenedWindow();

                // Update the last move
                if ((e.Reason == Game.Results.Timeout || e.Reason == Game.Results.Resignation) && m_Game.Moves.Count > 0) {
                    var move = m_Game.Moves.Last();
                    var stack = m_Moves.Children.FirstOrDefault(s => s.Name == $"move_{move.Index}") as StackPanel;

                    Control toRemove = stack?.Children.Last();
                    if (toRemove != null) {
                        stack.Children.Remove(toRemove);
                        AddMove(m_Chessboard, move);
                    }
                }

                var dlg = new GameEndedDialog(m_Game);
                if (await dlg.Show<bool?>(App.MainWindow) == true) {
                    // Rematch
                    var settings = m_Game.Settings;
                    settings.InitialFenPosition = string.Empty;
                    settings.Players[0].Color = settings.Players[0].Color == Game.Colors.White ? Game.Colors.Black : Game.Colors.White;
                    settings.Players[1].Color = settings.Players[1].Color == Game.Colors.White ? Game.Colors.Black : Game.Colors.White;

                    var game = new Game();
                    game.Init(settings);
                    await SetGame(game);
                } else {
                    SetAnalyzeMode();
                }
            });
        } // OnGameEnded

        #endregion

        #region Menu events
        private async void OnNewGameClick(object sender, RoutedEventArgs e)
        {
            if (App.Settings.Engines == null || App.Settings.Engines.Count == 0) {
                await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["NoEnginesError"], MessageDialog.Icons.Error);
                return;
            }

            var ngw = new NewGamePage();
            ngw.Navigating += async (s) =>
            {
                var newGame = ngw.Result;
                if (newGame != null) {
                    var game = new Game();
                    var settings = new Game.GameSettings()
                    {
                        IsChess960 = newGame.Chess960,
                        MaximumTime = newGame.MaxTime,
                        TimeIncrement = newGame.TimeIncrement,
                        TrainingMode = newGame.TrainingMode,
                        MaxEngineThinkingTime = TimeSpan.FromSeconds(App.Settings.MaxEngineThinkingTimeSecs),
                        EngineDepth = App.Settings.MaxEngineDepth,
                        InitialFenPosition = newGame.InitialPosition,
                    };

                    foreach (var player in newGame.Players) {
                        Player p;
                        if (player.IsHuman) {
                            p = new HumanPlayer(player.Color!.Value, player.Name, null);
                        } else {
                            var engine = App.Settings.GetEngine(player.EngineId)?.Copy();
                            if (player.EngineElo.HasValue)
                                engine.SetElo(player.EngineElo.Value);
                            var enginePlayer = new EnginePlayer(player.Color!.Value,
                                player.TheKingPersonality?.DisplayName ?? engine.Name,
                                engine.GetElo());
                            enginePlayer.Engine = engine;
                            enginePlayer.Personality = player.Personality;
                            enginePlayer.TheKingPersonality = player.TheKingPersonality;
                            enginePlayer.OpeningBookFileName = player.OpeningBook;

                            p = enginePlayer;
                        }
                        settings.Players.Add(p);
                    }

                    try {
                        game.Init(settings);
                    } catch (Exception) {
                        await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["InvalidFenString"], MessageDialog.Icons.Error);

                        settings.InitialFenPosition = string.Empty;
                        game.Init(settings);
                    }
                    await SetGame(game);
                }
            };
            await NavigateTo(ngw);
        } // OnNewGameClick

        private async void OnSaveGameClick(object sender, RoutedEventArgs e)
        {
            var files = await MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                AllowMultiple = false,
                FileTypeFilter = new []
                {
                    new FilePickerFileType("CoreChess save file (*.ccsf)")
                    {
                        Patterns = new []{ "*.ccsf" }
                    },
                    new FilePickerFileType("Portable Game Notation (*.pgn)")
                    {
                        Patterns = new []{ "*.pgn" }
                    }
                }
            });

            if (files.Count > 0) {
                var file = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);
                try {
                    if (Path.GetExtension(file) == ".pgn")
                        await m_Game.SaveToPgn(file);
                    else
                        await m_Game.Save(file);
                } catch (Exception ex) {
                    await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"],
                            string.Format(Localizer.Localizer.Instance["SaveGameError"], ex.Message), MessageDialog.Icons.Error);
                }
            }
        } // OnSaveGameClick

        private async void OnLoadGameClick(object sender, RoutedEventArgs e)
        {
            var files = await MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                AllowMultiple = false,
                FileTypeFilter = new []
                {
                    new FilePickerFileType("CoreChess save file")
                    {
                        Patterns = new []{ "*.ccsf" }
                    },
                    new FilePickerFileType("Portable Game Notation (*.pgn)")
                    {
                        Patterns = new []{ "*.pgn" }
                    }
                }
            });
            if (files.Count > 0) {
                await LoadGame(HttpUtility.UrlDecode(files[0].Path.AbsolutePath));
            }
        } // OnLoadGameClick

        private async void OnUndoMoveClick(object sender, RoutedEventArgs e)
        {
            if (!m_Game.Ended) {
                await m_Chessboard.UndoMove();
                m_Context.IsResignEnabled = CanResignOrPause();
                m_Context.CanPause = CanResignOrPause();
                UpdateCapturedPieces();
                await UpdateMoves();
            }
        } // OnUndoMoveClick

        private async void OnResignClick(object sender, RoutedEventArgs e)
        {
            if (!m_Game.Ended && await MessageDialog.ShowConfirmMessage(App.MainWindow, Localizer.Localizer.Instance["Confirm"], Localizer.Localizer.Instance["ResignGameConfirm"]))
                await m_Chessboard.ResignGame();
        } // OnFlipBoardClick

        private async void OnCopyPgnToClipboardClick(object sender, RoutedEventArgs e)
        {
            if (m_Game != null) {
                string tempFile = Path.GetTempFileName();
                if (await m_Game.SaveToPgn(tempFile)) {
                    using (StreamReader sr = new StreamReader(tempFile)) {
                        string pgn = await sr.ReadToEndAsync();
                        try {
                            await MainWindow!.Clipboard!.SetTextAsync(pgn);
                            App.MainWindow.ShowNotification(Localizer.Localizer.Instance["Message"], Localizer.Localizer.Instance["PgnCopied"]);
                        } catch (Exception ex) {
                            await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"], string.Format(Localizer.Localizer.Instance["ErrorCopyingString"], ex.Message), MessageDialog.Icons.Error);
                        }
                    }
                    File.Delete(tempFile);
                }
            }
        } // OnCopyPgnToClipboardClick

        private async void OnEngineSettingsClick(object sender, RoutedEventArgs e)
        {
            var dlg = new EngineSettingsWindow(m_Game);
            await dlg.Show(App.MainWindow);
        } // OnEngineSettingsClick

        private async void OnSaveToPngClick(object sender, RoutedEventArgs e)
        {
            var files = await MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                AllowMultiple = false,
                FileTypeFilter = new []
                {
                    new FilePickerFileType("PNG image")
                    {
                        Patterns = new []{ "*.png" }
                    }
                }
            });

            if (files.Count > 0) {
                m_Chessboard.SaveToPng(HttpUtility.UrlDecode(files[0].Path.AbsolutePath));
            }
        } // OnSaveToPngClick

        private async void OnCopyFenClick(object sender, RoutedEventArgs e)
        {
            try {
                await MainWindow!.Clipboard!.SetTextAsync(m_Game.GetFenString());
                App.MainWindow.ShowNotification(Localizer.Localizer.Instance["Message"], Localizer.Localizer.Instance["FenStringCopied"]);
            } catch (Exception ex) {
                await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"], string.Format(Localizer.Localizer.Instance["ErrorCopyingString"], ex.Message), MessageDialog.Icons.Error);
            }
        } // OnCopyFenClick

        private void OnFlipBoardClick(object sender, RoutedEventArgs e)
        {
            m_Chessboard.Flipped = !m_Chessboard.Flipped;
            m_Chessboard.Redraw();
        } // OnFlipBoardClick

        private async void OnGamesDatabaseClick(object sender, RoutedEventArgs e)
        {
            var wDlg = new WaitDialog(Localizer.Localizer.Instance["LoadingGames"]);
            var wTask = wDlg.Show(App.MainWindow);
            List<Game> games = new List<Game>();
            foreach (var f in Directory.GetFiles(App.GamesDatabasePath, "*.ccsf")) {
                var tempGame = await Game.Load(f);
                games.Add(tempGame);
            }
            wDlg.Close();
            var gDlg = new GamesDatabaseWindow(games);
            gDlg.Navigating += async (s) =>
            {
                var selGame = gDlg.SelectedGame;

                if (selGame != null) {
                    await SetGame(selGame);
                    DispatcherTimer.RunOnce(() =>
                    {
                        DisplayMove(m_Game.Moves.Count - 1);
                    }, TimeSpan.FromMilliseconds(100), DispatcherPriority.Background);
                }
            };
            await NavigateTo(gDlg);
        } // OnGamesDatabaseClick

        private async void OnEnginesClick(object sender, RoutedEventArgs e)
        {
            var dlg = new EnginesPage();
            await NavigateTo(dlg);
        } // OnEnginesClick

        private async void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingsPage();
            dlg.Navigating += async (s) =>
            {
                if (dlg.Result == true) {
                    App.MainWindow.Topmost = App.Settings.Topmost;
                    SetChessboardOptions();
                    m_Chessboard.Redraw();

                    if (m_Game != null) {
                        UpdateCapturedPieces();
                        await UpdateMoves();
                    }
                }
            };
            await NavigateTo(dlg);
        } // OnSettingsClick

        private async void OnCheckForUpdatesClick(object sender, RoutedEventArgs e)
        {
            m_Context.CheckingForUpdates = true;
            var updater = new Utils.AutoUpdater();
            if (await updater.CheckForUpdate(App.MainWindow, true)) {
                App.MainWindow.Close();
            }
            m_Context.CheckingForUpdates = false;
        } // OnCheckForUpdatesClick

        private async void OnAboutClick(object sender, RoutedEventArgs e)
        {
            var dlg = new AboutDialog();
            await dlg.Show(App.MainWindow);
        } // OnAboutClick

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            App.MainWindow.Close();
        } // OnExitClick
        #endregion

        #region Window events
        public override void HandleWindowStateChanged(WindowState state)
        {
            if (state == WindowState.Minimized && App.Settings.AutoPauseWhenMinimized && m_Game?.Status == Game.Statuses.InProgress) {
                OnPauseClick(null, new RoutedEventArgs());
            }

            base.HandleWindowStateChanged(state);
        } // HandleWindowStateChanged

        public override void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.P) {
                e.Handled = true;
                if (m_Context.CanPause) {
                    if (m_Game.Status == Game.Statuses.InProgress)
                        OnPauseClick(null, new RoutedEventArgs());
                    else if (m_Game.Status == Game.Statuses.Paused)
                        OnResumeClick(null, new RoutedEventArgs());
                }
            }

            if (m_Context.IsPaused || e.Handled)
                return;

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.N) {
                e.Handled = true;
                OnNewGameClick(null, new RoutedEventArgs());
            } else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.L) {
                e.Handled = true;
                OnLoadGameClick(null, new RoutedEventArgs());
            } else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.S) {
                e.Handled = true;
                OnSaveGameClick(null, new RoutedEventArgs());
            } else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.F) {
                e.Handled = true;
                OnFlipBoardClick(null, new RoutedEventArgs());
            } else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C) {
                e.Handled = true;
                OnCopyFenClick(null, new RoutedEventArgs());
            } else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.G) {
                e.Handled = true;
                OnCopyPgnToClipboardClick(null, new RoutedEventArgs());
            } else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Z) {
                if (m_Context.IsResignEnabled)
                    OnUndoMoveClick(null, new RoutedEventArgs());
            } else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.R) {
                e.Handled = true;
                if (m_Context.IsResignEnabled)
                    OnResignClick(null, new RoutedEventArgs());
            } else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.F1) {
                e.Handled = true;
                OnAboutClick(null, new RoutedEventArgs());
            } else if (e.KeyModifiers == KeyModifiers.Alt && e.Key == Key.F4) {
                e.Handled = true;
                App.MainWindow.Close();
            } else if (m_MoveNavigator.IsVisible && e.KeyModifiers == KeyModifiers.None && e.Key == Key.Left) {
                if (m_MovePrevious.IsEnabled) {
                    OnMoveNavigationClick(m_MovePrevious, new RoutedEventArgs());
                    e.Handled = true;
                }
            } else if (m_MoveNavigator.IsVisible && e.KeyModifiers == KeyModifiers.None && e.Key == Key.Home) {
                if (m_MoveFirst.IsEnabled) {
                    OnMoveNavigationClick(m_MoveFirst, new RoutedEventArgs());
                    e.Handled = true;
                }
            } else if (m_MoveNavigator.IsVisible && e.KeyModifiers == KeyModifiers.None && e.Key == Key.Right) {
                if (m_MoveNext.IsEnabled) {
                    OnMoveNavigationClick(m_MoveNext, new RoutedEventArgs());
                    e.Handled = true;
                }
            } else if (m_MoveNavigator.IsVisible && e.KeyModifiers == KeyModifiers.None && e.Key == Key.End) {
                if (m_MoveLast.IsEnabled) {
                    OnMoveNavigationClick(m_MoveLast, new RoutedEventArgs());
                    e.Handled = true;
                }
            } else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Z) {
                OnZenModeClick(null, new RoutedEventArgs());
                e.Handled = true;
            } else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.A) {
                if (m_ViewCommentBtn.IsEnabled) {
                    OnViewCommentBtnClick(m_ViewCommentBtn, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
        } // OnWindowKeyDown

        private async void OnWindowClosing(object sender, CancelEventArgs args)
        {
            if (m_Game != null) {
                args.Cancel = true;
                string autoSave = Path.Join(App.LocalPath, "autosave.ccsf");
                if (m_Game.Ended == false && m_Game.FullMoveNumber > 0 && App.Settings.AutoSaveGameOnExit) {
                    m_Game.WhiteTimeLeftMilliSecs = m_Game.LastWhiteTimeLeftMilliSecs;
                    m_Game.BlackTimeLeftMilliSecs = m_Game.LastBlackTimeLeftMilliSecs;
                    await m_Game.Save(autoSave);
                } else if (File.Exists(autoSave)) {
                    File.Delete(autoSave);
                }

                await m_Game.Stop();
                m_Game.Dispose();
                m_Game = null;

                if (!m_Context.ZenMode)
                    App.MainWindow.SaveWindowSizeAndPosition();

                App.MainWindow.Close();
            }
        } // OnWindowClosing
        #endregion

        #region Other events
        private void OnZenModeClick(object sender, RoutedEventArgs e)
        {
            m_Context.OnZenModeClick.Execute(this);
        } // OnZenModeClick

        private void OnPauseClick(object sender, RoutedEventArgs e)
        {
            if (m_Chessboard.Game.Pause()) {
                m_Context.IsPaused = true;
            }
        } // OnPauseClick

        private void OnResumeClick(object sender, RoutedEventArgs e)
        {
            m_Chessboard.Game.Resume();
            m_Context.IsPaused = false;
            if (m_Context.ZenMode) {
                m_SidePanel.IsVisible = false;
                m_Menu.IsVisible = false;
            } else {
                m_SidePanel.IsVisible = true;
                m_Menu.IsVisible = true;
            }
        } // OnResumeClick

        private void OnMoveTapped(object sender, RoutedEventArgs e)
        {
            var move = (sender as TextBlock).DataContext as Game.MoveNotation;
            DisplayMove(m_Game.Moves.IndexOf(move));
        } // OnMoveTapped

        private async void OnMoveDoubleTapped(object sender, RoutedEventArgs e)
        {
            var move = (sender as TextBlock).DataContext as Game.MoveNotation;
            var dlg = new MoveCommentDialog(move);

            if (await dlg.Show<bool?>(App.MainWindow) == true) {
                await UpdateMoves();
                if (!string.IsNullOrEmpty(m_Game.FileName)) {
                    try {
                        await m_Game.Save(m_Game.FileName);
                    } catch {
                        // ignored
                    }
                }
            }
        } // OnMoveDoubleTapped

        private void OnViewCommentBtnClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (m_CurrentMoveIndex.HasValue && m_CurrentMoveIndex >= 0) {
                var move = m_Game.Moves[m_CurrentMoveIndex.Value];
                TextBlock moveTxt = new TextBlock() { DataContext = move };
                OnMoveDoubleTapped(moveTxt, new RoutedEventArgs());
            }
        } // OnViewCommentBtnClick

        private void OnMouseOnAnalysisResult(object sender, GameAnalyzeGraph.MouseEventArgs args)
        {
            var move = args.Index.HasValue ? m_Game.Moves[args.Index.Value - 1] : null;

            // Highlight the move
            foreach (var child in m_Moves.Children) {
                var stack = child as StackPanel;
                foreach (var t in stack.Children) {
                    if (move != null && t.DataContext == move) {
                        t.Classes.Remove("HiglightOnOver");
                        t.Classes.Add("HiglightBackground");
                    } else {
                        t.Classes.Remove("HiglightBackground");
                        t.Classes.Add("HiglightOnOver");
                    }
                }
            }
        } // OnMouseOnAnalysisResult

        private void OnMouseClickOnAnalysisResult(object sender, GameAnalyzeGraph.MouseEventArgs args)
        {
            if (args.Index.HasValue)
                DisplayMove(args.Index.Value - 1);
        } // OnMouseClickOnAnalysisResult

        private void OnMoveNavigationClick(object sender, RoutedEventArgs e)
        {
            if (m_Game != null) {
                var btn = sender as Button;
                if (btn.Name == "m_MoveFirst") {
                    DisplayMove(-1);
                } else if (btn.Name == "m_MovePrevious") {
                    DisplayMove(m_CurrentMoveIndex.Value - 1);
                } else if (btn.Name == "m_MoveNext") {
                    DisplayMove(m_CurrentMoveIndex.Value + 1);
                } else if (btn.Name == "m_MoveLast") {
                    DisplayMove(m_Game.Moves.Count - 1);
                }
            }
        } // OnMoveNavigationClick
        #endregion

        #region private operations
        private async void InitializeWindow(object sender, VisualTreeAttachmentEventArgs e)
        {
            if (m_Initialized)
                return;
            m_Initialized = true;

            SetWaitAnimation(true);

            // Show engine output setting
            m_Context.ShowEngineOutput = App.Settings.ShowEngineOutput;
            m_Context.ZenMode = false;

            Game game = null;

            // Check arguments
            if (m_Args != null && m_Args.Length > 0) {
                if (File.Exists(m_Args[0])) {
                    try {
                        game = await Game.Load(m_Args[0]);
                    } catch {
                        // ignored
                    }
                }
            } else {
                // Check autosave
                string autoSave = Path.Join(App.LocalPath, "autosave.ccsf");
                if (File.Exists(autoSave)) {
                    try {
                        game = await Game.Load(autoSave);
                    } catch {
                        // ignored
                    }

                    File.Delete(autoSave);
                }
            }

            if (game == null) {
                game = new Game();
                Game.GameSettings settings;
                if (App.Settings.NewGame != null) {
                    // Reuse last new game settings
                    settings = new Game.GameSettings()
                    {
                        MaxEngineThinkingTime = TimeSpan.FromSeconds(App.Settings.MaxEngineThinkingTimeSecs),
                        EngineDepth = App.Settings.MaxEngineDepth,
                        MaximumTime = App.Settings.NewGame.MaxTime
                    };

                    foreach (var p in App.Settings.NewGame.Players) {
                        if (p.IsHuman) {
                            settings.Players.Add(new HumanPlayer(p.Color!.Value, p.Name, null));
                        } else {
                            var engine = App.Settings.GetEngine(p.EngineId) ?? App.Settings.Engines?.FirstOrDefault();;
                            var enginePlayer =
                                new EnginePlayer(p.Color!.Value, engine?.Name, p.EngineElo)
                                {
                                    Engine = engine?.Copy(),
                                    OpeningBookFileName = App.Settings.DefaultOpeningBook
                                };
                            enginePlayer.Engine.SetElo(p.EngineElo!.Value);
                            settings.Players.Add(enginePlayer);
                        }
                    }
                } else {
                    settings = new Game.GameSettings()
                    {
                        MaxEngineThinkingTime = TimeSpan.FromSeconds(App.Settings.MaxEngineThinkingTimeSecs),
                        EngineDepth = App.Settings.MaxEngineDepth,
                        MaximumTime = TimeSpan.FromMinutes(15)
                    };
                    settings.Players.Add(new HumanPlayer(Game.Colors.White, App.Settings.PlayerName, null));
                    var engine = App.Settings.Engines?.FirstOrDefault();
                    var enginePlayer =
                        new EnginePlayer(Game.Colors.Black, engine?.Name, engine?.GetElo())
                        {
                            Engine = engine?.Copy(),
                            OpeningBookFileName = App.Settings.DefaultOpeningBook
                        };
                    settings.Players.Add(enginePlayer);
                }
                game.Init(settings);
            }

            await SetGame(game);

            // Check for update (Windows only)
            if (OperatingSystem.IsWindows()) {
                DispatcherTimer.RunOnce(async () =>
                {
                    m_Context.CheckingForUpdates = true;
                    var updater = new Utils.AutoUpdater();
                    if (await updater.CheckForUpdate(App.MainWindow)) {
                        App.MainWindow.Close();
                    }
                    m_Context.CheckingForUpdates = false;
                }, TimeSpan.FromSeconds(2), DispatcherPriority.Background);
            }
        } // InitializeWindow

        private async Task<bool> LoadGame(string filePath)
        {
            if (Path.GetExtension(filePath) == ".pgn") {
                var wDlg = new WaitDialog(Localizer.Localizer.Instance["LoadingPGN"]);
                var wTask = wDlg.Show(App.MainWindow);
                List<PGN> games = null;
                try {
                    games = await PGN.LoadFile(filePath);
                } catch (Exception ex) {
                    wDlg.Close();
                    await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"],
                        string.Format(Localizer.Localizer.Instance["LoadPgnError"], ex.Message), MessageDialog.Icons.Error);
                    return false;
                }
                wDlg.Close();

                Game game = null;
                if (games.Count == 0)
                    await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["NoGameFoundInPgn"], MessageDialog.Icons.Error);
                else if (games.Count == 1)
                    game = await Game.LoadFromPgn(games[0], true);
                else {
                    var gDlg = new PgnGamesPage(games);
                    gDlg.Navigating += async (s) =>
                    {
                        var selGame = gDlg.SelectedGame;
                        if (selGame != null)
                            game = await Game.LoadFromPgn(selGame, false);

                        if (game != null) {
                            game.Settings.MaxEngineThinkingTime = TimeSpan.FromSeconds(App.Settings.MaxEngineThinkingTimeSecs);
                            game.Settings.EngineDepth = App.Settings.MaxEngineDepth;
                            await SetGame(game);
                            App.Settings.AddRecentlyLoadedFile(filePath);
                            App.Settings.Save(App.SettingsPath);
                            SetMru();
                        }
                    };
                    await NavigateTo(gDlg);
                }
            } else {
                try {
                    await SetGame(await Game.Load(filePath));
                    App.Settings.AddRecentlyLoadedFile(filePath);
                    App.Settings.Save(App.SettingsPath);
                    SetMru();
                } catch {
                    await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["LoadGameError"], MessageDialog.Icons.Error);
                }
            }

            return true;
        } // LoadGame

        private void SetPlayerToMove()
        {
            if (m_Game.Ended) {
                m_WhiteBorder.Classes.Remove("Selected");
                m_WhiteName.Classes.Remove("HighlightColor");
                m_BlackBorder.Classes.Remove("Selected");
                m_BlackName.Classes.Remove("HighlightColor");
            } else if (m_Game.ToMove == Game.Colors.White) {
                m_WhiteBorder.Classes.Add("Selected");
                m_WhiteName.Classes.Add("HighlightColor");
                m_BlackBorder.Classes.Remove("Selected");
                m_BlackName.Classes.Remove("HighlightColor");
            } else {
                m_WhiteBorder.Classes.Remove("Selected");
                m_WhiteName.Classes.Remove("HighlightColor");
                m_BlackBorder.Classes.Add("Selected");
                m_BlackName.Classes.Add("HighlightColor");
            }
        } // SetPlayerToMove

        private void UpdateCapturedPieces()
        {
            if (m_Game == null)
                return;

            var capturedPieces = m_Game.GetCapturedPieces(m_CurrentMoveIndex.HasValue ? m_CurrentMoveIndex.Value : m_Game.Moves.Count - 1);
            var wPieces = capturedPieces.Where(p => p.Color == Game.Colors.White).OrderByDescending(p => p.Value).ThenBy(p => p.Acronym).ToList();
            var bPieces = capturedPieces.Where(p => p.Color == Game.Colors.Black).OrderByDescending(p => p.Value).ThenBy(p => p.Acronym).ToList();
            int wValue = m_Game.Board.GetPieces(Game.Colors.White).Where(p => p.Type != Piece.Pieces.King).Sum(p => p.Value);
            int bValue = m_Game.Board.GetPieces(Game.Colors.Black).Where(p => p.Type != Piece.Pieces.King).Sum(p => p.Value);

            var updateNeeded = !m_LastWhiteCapturedPieces.SequenceEqual(wPieces.Select(p => p.Type)) || !m_LastBlackCapturedPieces.SequenceEqual(bPieces.Select(p => p.Type));
            if (!updateNeeded)
                return;

            m_LastWhiteCapturedPieces = wPieces.Select(p => p.Type).ToList();
            m_LastBlackCapturedPieces = bPieces.Select(p => p.Type).ToList();

            if (App.Settings.CapturedPieces == Settings.CapturedPiecesDisplay.Difference) {
                var tempWhite = new List<Piece>();
                var tempBlack = new List<Piece>();

                for (int i = 0; i < wPieces.Count; i++) {
                    var wp = wPieces[i];
                    var bp = bPieces.FirstOrDefault(p => p.Type == wp.Type);
                    if (bp == null)
                        tempWhite.Add(wp);
                    else {
                        bPieces.Remove(bp);
                        wPieces.RemoveAt(i--);
                    }
                }

                for (int i = 0; i < bPieces.Count; i++) {
                    var bp = bPieces[i];
                    var wp = wPieces.FirstOrDefault(p => p.Type == bp.Type);
                    if (wp == null)
                        tempBlack.Add(bp);
                    else {
                        wPieces.Remove(wp);
                        bPieces.RemoveAt(i--);
                    }
                }

                wPieces = tempWhite;
                bPieces = tempBlack;
            }

            m_WhiteCapturedPieces.Children.Clear();
            Piece lastPiece = null;
            foreach (var cp in bPieces) {
                string fileName = $"{m_Chessboard.PiecesFolder}{Path.DirectorySeparatorChar}b{cp.Type.ToString()}.png";
                var bitmap = new Bitmap(fileName);

                Thickness margin = new Thickness(0);
                if (lastPiece?.Type == cp.Type)
                    margin = new Thickness(-10, 0, 0, 0);
                m_WhiteCapturedPieces.Children.Add(new Image() { Source = bitmap, Height = 35, Margin = margin });
                lastPiece = cp;
            }

            m_BlackCapturedPieces.Children.Clear();
            lastPiece = null;
            foreach (var cp in wPieces) {
                string fileName = $"{m_Chessboard.PiecesFolder}{Path.DirectorySeparatorChar}w{cp.Type.ToString()}.png";
                var bitmap = new Bitmap(fileName);

                Thickness margin = new Thickness(0);
                if (lastPiece?.Type == cp.Type)
                    margin = new Thickness(-10, 0, 0, 0);
                m_BlackCapturedPieces.Children.Add(new Image() { Source = bitmap, Height = 35, Margin = margin });
                lastPiece = cp;
            }

            if (bValue > wValue) {
                m_BlackCapturedPieces.Children.Add(new TextBlock()
                {
                    Text = $"+{bValue - wValue}",
                    Margin = new Thickness(5),
                    FontSize = 16,
                    FontWeight = FontWeight.Light,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                });
            } else if (wValue > bValue) {
                m_WhiteCapturedPieces.Children.Add(new TextBlock()
                {
                    Text = $"+{wValue - bValue}",
                    Margin = new Thickness(5),
                    FontSize = 16,
                    FontWeight = FontWeight.Light,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                });
            }
        } // UpdateCapturedPieces

        private async Task<bool> UpdateMoves()
        {
            m_Moves.Children.Clear();

            using (var game = new Game()) {
                var settings = new Game.GameSettings();
                settings.InitialFenPosition = m_Game.Settings.InitialFenPosition;
                settings.IsChess960 = m_Game.Settings.IsChess960;
                settings.Players.Add(new HumanPlayer(Game.Colors.White, string.Empty, null));
                settings.Players.Add(new HumanPlayer(Game.Colors.Black, string.Empty, null));
                game.Init(settings);

                int idx = 0;
                foreach (var m in m_Game.Moves) {
                    // Backward compatibility
                    if (m.Index == 0)
                        m.Index = idx / 2 + 1;
                    if (m.Color == null)
                        m.Color = idx % 2 == 0 ? Game.Colors.White : Game.Colors.Black;

                    await game.DoMove(m.Coordinate, false, true);

                    // Create a new Chessboard so we can create the image using a DispatcherTimer
                    var tempBoard = new Chessboard();
                    tempBoard.PiecesFolder = App.GetPiecesPath(App.Settings.PiecesSet);
                    tempBoard.SquareWhiteColor = Utils.ColorConverter.ParseHexColor(App.Settings.WhiteColor);
                    tempBoard.SquareWhiteSelectedColor = Utils.ColorConverter.ParseHexColor(App.Settings.WhiteSelectedColor);
                    tempBoard.SquareBlackColor = Utils.ColorConverter.ParseHexColor(App.Settings.BlackColor);
                    tempBoard.SquareBlackSelectedColor = Utils.ColorConverter.ParseHexColor(App.Settings.BlackSelectedColor);
                    tempBoard.ShowFileRankNotation = Settings.FileRankNotations.None;
                    await tempBoard.SetGame(game.Copy());
                    tempBoard.Flipped = m_Game.GetPlayer(Game.Colors.Black) is HumanPlayer && m_Game.GetPlayer(Game.Colors.White) is EnginePlayer;

                    AddMove(tempBoard, m, true);
                    idx++;
                }
            }
            if (!m_Game.Settings.IsChess960)
                UpdateEco();
            if (m_CurrentMoveIndex.HasValue)
                DisplayMove(m_CurrentMoveIndex.Value);
            return true;
        } // UpdateMoves

        /// <summary>
        /// Add a move to the list.
        /// </summary>
        /// <param name="chessboard"></param>
        /// <param name="move"></param>
        /// <param name="disposeGame"></param>
        /// <returns>The StackPanel containing the move text</returns>
        private StackPanel AddMove(Chessboard chessboard, Game.MoveNotation move, bool disposeGame = false)
        {
            var stack = m_Moves.Children.FirstOrDefault(s => s.Name == $"move_{move.Index}") as StackPanel;
            if (stack == null) {
                stack = new StackPanel() { Name = $"move_{move.Index}", Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Thickness(0, 0, 5, 0) };
                m_Moves.Children.Add(stack);
            }

            bool isWhite = move.Color == Game.Colors.White;
            List<string> classes = new List<string> { "HiglightOnOver" };
            bool isFigurine = App.Settings.MoveNotation == Settings.Notations.FigurineShortAlgebraic || App.Settings.MoveNotation == Settings.Notations.FigurineLongAlgebraic;
            if (isFigurine)
                classes.Add("Figurine");

            string notation = string.Empty;
            switch (App.Settings.MoveNotation) {
                case Settings.Notations.ShortAlgebraic:
                case Settings.Notations.FigurineShortAlgebraic:
                    notation = move.ShortAlgebraic;
                    break;
                case Settings.Notations.LongAlgebraic:
                case Settings.Notations.FigurineLongAlgebraic:
                    notation = move.LongAlgebraic;
                    break;
                case Settings.Notations.Coordinate:
                    notation = $"{move.Coordinate.Substring(0, 2)}-{move.Coordinate.Substring(2)}";
                    break;
            }

            if (isFigurine) {
                // Letters are inverted because we are using a dark theme
                if (notation.Contains("K", StringComparison.InvariantCulture))
                    notation = notation.Replace("K", isWhite ? "N" : "n", StringComparison.InvariantCulture);
                else if (notation.Contains("Q", StringComparison.InvariantCulture))
                    notation = notation.Replace("Q", isWhite ? "M" : "m", StringComparison.InvariantCulture);
                else if (notation.Contains("R", StringComparison.InvariantCulture))
                    notation = notation.Replace("R", isWhite ? "L" : "l", StringComparison.InvariantCulture);
                else if (notation.Contains("N", StringComparison.InvariantCulture))
                    notation = notation.Replace("N", isWhite ? "K" : "k", StringComparison.InvariantCulture);
                else if (notation.Contains("B", StringComparison.InvariantCulture))
                    notation = notation.Replace("B", isWhite ? "J" : "j", StringComparison.InvariantCulture);
                else if (notation.Contains("P", StringComparison.InvariantCulture))
                    notation = notation.Replace("P", isWhite ? "I" : "i", StringComparison.InvariantCulture);
            }

            if (isWhite) {
                var numberTxt = new TextBlock() { Text = $"{move.Index}.", Margin = new Thickness(0, 2, 0, 2), MinWidth = 25, FontWeight = FontWeight.Regular };
                if (isFigurine)
                    numberTxt.Classes.Add("Figurine");
                stack.Children.Add(numberTxt);
            }

            TextBlock moveTxt = new TextBlock() { DataContext = move, Text = $"{notation}{move.Annotation}", Margin = new Thickness(0, 2, 5, 2), MinWidth = 50, FontWeight = FontWeight.Light };
            moveTxt.Classes.AddRange(classes);
            moveTxt.Tapped += OnMoveTapped;
            moveTxt.DoubleTapped += OnMoveDoubleTapped;
            stack.Children.Add(moveTxt);

            // Tooltip
            var tStack = new StackPanel()
            {
                Width = 180,
                Orientation = Avalonia.Layout.Orientation.Vertical
            };
            var img = new Image()
            {
                Width = 180,
                Height = 180
            };

            img.AttachedToVisualTree += (s, e) =>
            {
                var image = s as Image;
                if (image.Source == null)
                    image.Source = chessboard.GetBitmap(new Size(300, 300), move);
            };
            if (disposeGame)
                chessboard.Game.Dispose();

            tStack.Children.Add(img);
            ToolTip.SetTip(moveTxt, tStack);

            if (!string.IsNullOrEmpty(move.Comment)) {
                TextBlock txt = new TextBlock()
                {
                    Text = move.Comment,
                    MaxHeight = 120,
                    TextWrapping = TextWrapping.Wrap,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                tStack.Children.Add(txt);
                moveTxt.FontWeight = FontWeight.Bold;
            }

            return stack;
        } // AddMove

        private void UpdateEco()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var m in m_Game.Moves) {
                sb.Append($"{m.ShortAlgebraic} ");
            }

            string ecoMoves = sb.ToString().Trim();
            if (m_Game.Moves.Count > 1 && !string.IsNullOrEmpty(ecoMoves)) {
                Utils.EcoDatabase.Eco foundEco = m_EcoDatabase.GetByMoves(ecoMoves);
                if (foundEco != null) {
                    m_Game.ECO = foundEco.Code;
                    m_Context.EcoName = foundEco.FullName;
                } else if (!m_Game.Ended && m_Game.Moves.Count > 10) {
                    m_Context.EcoName = string.Empty;
                }
            }
        } // UpdateEco

        private async Task<bool> SetGame(Game game)
        {
            await m_GameGraph.Abort();
            m_GameGraph.Clear();

            if (App.Settings.Engines == null || App.Settings.Engines.Count == 0) {
                m_Chessboard.SetEmpty();
                SetChessboardOptions();
                SetWaitAnimation(false);
                await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["NoEnginesError"], MessageDialog.Icons.Error);
                return false;
            }

            SetWaitAnimation(true);
            await Task.Delay(1);

            if (m_Game != null) {
                await m_Game.Stop();
                m_Game.Dispose();
            }

            m_LastWhiteCapturedPieces = new List<Piece.Pieces>();
            m_LastBlackCapturedPieces = new List<Piece.Pieces>();

            // Clear GUI:
            m_WhiteCapturedPieces.Children.Clear();
            m_BlackCapturedPieces.Children.Clear();
            m_Moves.Children.Clear();
            m_EngineMessagesRows.Clear();
            m_EngineMessage.Text = string.Empty;

            // Reset time
            m_Context.WhiteTime = "00:00";
            m_Context.BlackTime = "00:00";

            m_Context.EcoName = string.Empty;

            m_Game = game;

            // Set players name
            m_Context.WhiteName = m_Game.Settings.WhitePlayerName;
            m_Context.WhiteElo = m_Game.Settings.WhitePlayer?.Elo;
            m_Context.BlackName = m_Game.Settings.BlackPlayerName;
            m_Context.BlackElo = m_Game.Settings.BlackPlayer?.Elo;

            m_ViewCommentBtn.Classes.Remove("Selected");
            if (m_Game.Ended) {
                SetAnalyzeMode();
            } else {
                m_CurrentMoveIndex = null;
                m_MoveNavigator.IsVisible = false;
                m_GameAnalyzeSection.IsVisible = false;
                m_EngineMessageSection.IsVisible = App.Settings.ShowEngineOutput;
                m_WhiteTimeLeft.IsVisible = true;
                m_BlackTimeLeft.IsVisible = true;
                m_PauseBtn.IsVisible = true;
                m_ViewCommentBtn.IsVisible = false;
            }

            m_Context.IsEngineSettingsEnabled = m_Game.Settings.Players.Any(p => p is EnginePlayer);
            m_Context.CanPause = CanResignOrPause();
            m_Context.IsResignEnabled = CanResignOrPause();
            SetChessboardOptions();

            try {
                var res = await m_Chessboard.SetGame(m_Game);
                if (res) {
                    DispatcherTimer.RunOnce(() =>
                    {
                        m_Chessboard.StartGame();
                    }, TimeSpan.FromMilliseconds(100), DispatcherPriority.Background);
                }
                return res;
            } catch (Exception ex) {
                SetWaitAnimation(false);
                m_Chessboard.SetEmpty();
                await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"], string.Format(Localizer.Localizer.Instance["ErrorStartingEngine"], ex.Message, MessageDialog.Icons.Error));
            } finally {
                SetWaitAnimation(false);
            }
            return false;
        } // SetGame

        private void SetChessboardOptions()
        {
            m_Chessboard.EnableDragAndDrop = App.Settings.EnableDragAndDrop;
            m_Chessboard.EnableAudio = App.Settings.EnableAudio;
            m_Chessboard.ShowAvailableMoves = App.Settings.ShowValidMoves;
            m_Chessboard.ShowFileRankNotation = App.Settings.ShowFileRankNotation;

            m_Chessboard.PiecesFolder = App.GetPiecesPath(App.Settings.PiecesSet);
            m_Chessboard.BorderColor = (Color)this.FindResource("SectionBackgroundColor");
            m_Chessboard.SquareWhiteColor = Utils.ColorConverter.ParseHexColor(App.Settings.WhiteColor);
            m_Chessboard.SquareWhiteSelectedColor = Utils.ColorConverter.ParseHexColor(App.Settings.WhiteSelectedColor);
            m_Chessboard.SquareBlackColor = Utils.ColorConverter.ParseHexColor(App.Settings.BlackColor);
            m_Chessboard.SquareBlackSelectedColor = Utils.ColorConverter.ParseHexColor(App.Settings.BlackSelectedColor);

            m_WhiteBorder.Background = new SolidColorBrush(Utils.ColorConverter.ParseHexColor(App.Settings.BlackColor));
            m_White.Source = new Bitmap(Path.Join(m_Chessboard.PiecesFolder, "wKing.png"));

            m_BlackBorder.Background = new SolidColorBrush(Utils.ColorConverter.ParseHexColor(App.Settings.WhiteColor));
            m_Black.Source = new Bitmap(Path.Join(m_Chessboard.PiecesFolder, "bKing.png"));
        } // SetChessboardOptions

        private void SetAnalyzeMode()
        {
            m_Context.IsResignEnabled = CanResignOrPause();
            m_Context.CanPause = CanResignOrPause();
            m_PauseBtn.IsVisible = false;
            m_WhiteTimeLeft.IsVisible = false;
            m_BlackTimeLeft.IsVisible = false;
            m_MoveNavigator.IsVisible = true;
            m_ViewCommentBtn.IsVisible = true;

            m_MoveFirst.IsEnabled = true;
            m_MovePrevious.IsEnabled = true;
            m_MoveNext.IsEnabled = false;
            m_MoveLast.IsEnabled = false;

            m_CurrentMoveIndex = -1;
            m_GameAnalyzeSection.IsVisible = true;
            m_EngineMessageSection.IsVisible = false;

            m_GameGraph.Clear();
            m_GameGraph.Game = m_Game;
            m_GameGraph.AnalyzeCompleted += async (s, args) =>
            {
                // Save the analysis data
                if (!string.IsNullOrEmpty(m_GameGraph.Game.FileName)) {
                    try {
                        await m_GameGraph.Game.Save(m_GameGraph.Game.FileName);
                        DisplayMove(m_Game.Moves.Count - 1);
                    } catch {
                        // ignored
                    }
                }
            };

            // Get ECO
            m_Context.EcoName = string.Empty;
            var eco = m_EcoDatabase.GetByMoves(m_Game.Moves);
            if (eco != null)
                m_Context.EcoName = eco.FullName;

            if (m_Game.AnalyzeResults == null) {
                if (App.Settings.GameAnalysisEngine == null)
                    m_GameGraph.IsVisible = false;
                else if (App.Settings.AutoAnalyzeGames)
                    m_GameGraph.Analyze(App.Settings.GameAnalysisEngine.GetDefaultAnalyzeDepth());
            } else {
                m_GameGraph.SetResults(m_Game.AnalyzeResults);
                DisplayMove(m_Game.Moves.Count - 1);
            }
        } // SetAnalyzeMode

        /// <summary>
        /// Display the given move on the chessboard
        /// </summary>
        /// <param name="moveIndex">The game move index (if equal to -1 the starting position in shown)</param>
        private bool DisplayMove(int moveIndex)
        {
            if (m_Game != null && m_Game.Ended && moveIndex < m_Game.Moves.Count) {
                m_CurrentMoveIndex = moveIndex;
                m_ViewCommentBtn.Classes.Remove("Selected");
                Game.MoveNotation move = null;
                if (moveIndex == -1) {
                    m_Game.Board.InitFromFenString(m_Game.InitialFenPosition);
                } else {
                    move = m_Game.Moves[moveIndex];
                    m_Game.Board.InitFromFenString(move.Fen);
                }
                m_Chessboard.Redraw(move);

                if (moveIndex == -1)
                    m_GameGraph.RemoveMarker();
                else
                    m_GameGraph.AddMarker(moveIndex + 1);

                foreach (var child in m_Moves.Children) {
                    var stack = child as StackPanel;
                    foreach (var t in stack.Children) {
                        if (move != null && t.DataContext == move) {
                            t.Classes.Add("CurrentMove");
                            stack.BringIntoView();
                            string moveTxt = move.Comment;
                            if (!string.IsNullOrEmpty(moveTxt))
                                m_ViewCommentBtn.Classes.Add("Selected");
                        } else
                            t.Classes.Remove("CurrentMove");
                    }
                }

                UpdateCapturedPieces();

                m_MoveFirst.IsEnabled = moveIndex >= 0;
                m_MovePrevious.IsEnabled = moveIndex >= 0;
                m_MoveNext.IsEnabled = moveIndex < m_Game.Moves.Count - 1;
                m_MoveLast.IsEnabled = moveIndex < m_Game.Moves.Count - 1;

                return true;
            }

            return false;
        } // DisplayMove

        private void SetWaitAnimation(bool visible)
        {
            m_Wait.IsVisible = visible;
            if (visible)
                m_WaitSpinner.Classes.Add("spinner");
            else
                m_WaitSpinner.Classes.Remove("spinner");
        } // SetWaitAnimation

        private void SetMru()
        {
            var items = new List<MenuItem>();
            if (App.Settings.RecentlyLoadedFiles != null) {
                for (var i = App.Settings.RecentlyLoadedFiles.Count - 1; i >= 0; i--) {
                    var mrue = App.Settings.RecentlyLoadedFiles[i];
                    var item = new MenuItem()
                    {
                        Header = mrue
                    };
                    item.Click += async (s, a) =>
                    {
                        var menuItem = s as MenuItem;
                        await LoadGame((string)menuItem.Header);
                    };
                    items.Add(item);
                }
            }

            m_Mru.ItemsSource = items;
            m_Mru.IsVisible = items.Count > 0;
        }

        private bool CanResignOrPause()
        {
            if (m_Game == null)
                return false;
            return m_Game.Settings.Players.Any(p => p is HumanPlayer) &&
                   m_Game.FullMoveNumber > 0 && !m_Game.Ended;
        }
        #endregion
    }
}
