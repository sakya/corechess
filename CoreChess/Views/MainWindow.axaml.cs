using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ChessLib;
using ChessLib.Engines;
using CoreChess.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CoreChess.Views
{
    public class MainWindow : BaseView
    {
        #region classes
        class Context : INotifyPropertyChanged
        {
            #region commands
            class MoveNotationCommand : ICommand
            {
                Context m_Owner = null;
                public event EventHandler CanExecuteChanged {
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
                    if (Enum.TryParse<Settings.Notations>((string)parameter, out notation)) {
                        App.Settings.MoveNotation = notation;
                        App.Settings.Save(App.SettingsPath);
                        m_Owner.MoveNotation = notation;
                        await m_Owner.Window.UpdateMoves();
                    }
                }
            }

            class CapturedPiecesCommand : ICommand
            {
                Context m_Owner = null;
                public event EventHandler CanExecuteChanged {
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
                    if (Enum.TryParse<Settings.CapturedPiecesDisplay>((string)parameter, out setting)) {
                        App.Settings.CapturedPieces = setting;
                        App.Settings.Save(App.SettingsPath);
                        m_Owner.CapturedPieces = setting;
                        m_Owner.Window.UpdateCapturedPieces();
                    }
                }
            }

            class ShowEngineOutputCommand : ICommand
            {
                Context m_Owner = null;
                public event EventHandler CanExecuteChanged {
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

                    if (!m_Owner.Window.FindControl<Border>("m_GameAnalyzeSection").IsVisible)
                        m_Owner.Window.FindControl<Border>("m_EngineMessageSection").IsVisible = App.Settings.ShowEngineOutput;
                }
            }
            #endregion

            bool m_IsResignEnabled = true;
            bool m_CanPause = true;
            bool m_IsPaused = false;
            bool m_IsWindows = false;
            Settings.Notations? m_MoveNotation = null;
            Settings.CapturedPiecesDisplay? m_CapturedPieces = null;
            bool m_ShowEngineOutput = false;
            string m_WhiteName = string.Empty;
            int? m_WhiteElo;
            string m_BlackName = string.Empty;
            int? m_BlackElo;
            string m_WhiteTime = string.Empty;
            string m_BlackTime = string.Empty;
            string m_EcoName = string.Empty;

            public event PropertyChangedEventHandler PropertyChanged;

            public Context(MainWindow window)
            {
                Window = window;
                IsResignEnabled = true;
                CanPause = true;
                OnMoveNotationClick = new MoveNotationCommand(this);
                OnCapturedPiecesClick = new CapturedPiecesCommand(this);
                OnShowEngineOutputClick = new ShowEngineOutputCommand(this);
            }

            public MainWindow Window { get; private set; }

            public bool IsWindows
            {
                get { return m_IsWindows; }
                set { SetIfChanged(ref m_IsWindows, value); }
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

        class Eco {
            public string Code { get; set; }
            public string Moves { get; set; }
            public string Name { get; set; }
            public string Variation { get; set; }
        } // Eco
        #endregion

        string[] m_args = null;
        Context m_Context = null;
        Game m_Game = null;
        Grid m_Wait = null;
        Chessboard m_Chessboard = null;
        TextBlock m_EngineMessage = null;
        List<string> m_EngineMessagesRows = new List<string>();
        Dictionary<string, Eco> m_EcoDatabase = null;
        int? m_CurrentMoveIndex = null;
        WindowNotificationManager m_NotificationManager = null;

        public MainWindow()
        {
        }

        public MainWindow(string[] args)
        {
            m_args = args;
            InitializeComponent();
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();

            m_Context = new Context(this);
            m_Context.IsWindows = OperatingSystem.IsWindows();
            m_Context.MoveNotation = App.Settings.MoveNotation;
            m_Context.CapturedPieces = App.Settings.CapturedPieces;
            this.DataContext = m_Context;

            m_Wait = this.FindControl<Grid>("m_Wait");
            m_Chessboard = this.FindControl<Chessboard>("m_Chessboard");
            m_EngineMessage = this.FindControl<TextBlock>("m_EngineMessage");

            m_Wait.AttachedToVisualTree += InitializeWindow;

            m_NotificationManager = new WindowNotificationManager(this)
            {
                Position = NotificationPosition.TopRight,
                MaxItems = 3,
                Margin = OperatingSystem.IsWindows() ?  new Thickness(0, 30, 0, 0) : new Thickness(0)
            };
        }

        public async Task<bool> LoadEcoDatabase()
        {
            // Load ECO database
            StringBuilder sb = new StringBuilder();
            m_EcoDatabase = new Dictionary<string, Eco>();
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            Uri uri = new Uri($"avares://CoreChess/Assets/eco.pgn");
            using (Stream s = assets.Open(uri))
            {
                var pgnGames = await PGN.LoadFromStream(s);
                foreach (var pg in pgnGames)
                {
                    Eco eco = new Eco()
                    {
                        Name = pg.White,
                        Variation = pg.Black,
                        Code = pg.ECO
                    };

                    sb.Clear();
                    foreach (var m in pg.Moves)
                    {
                        sb.Append($"{m.Notation} ");
                    }
                    eco.Moves = sb.ToString().Trim();
                    m_EcoDatabase[eco.Moves] = eco;
                }
            }

            return true;
        } // LoadEcoDatabase

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
                else  {
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
                await MessageWindow.ShowMessage(this, Localizer.Localizer.Instance["Error"], error, MessageWindow.Icons.Error);
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
            m_Context.IsResignEnabled = m_Game.FullmoveNumber > 0;
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
                chessboard.ShowFileRankNotation = false;
                await chessboard.SetGame(game);
                chessboard.Flipped = m_Game.GetPlayer(Game.Colors.Black) is HumanPlayer && m_Game.GetPlayer(Game.Colors.White) is EnginePlayer;

                foreach (var m in m_Game.Moves) {
                    await game.DoMove(m.Coordinate, false, true);
                }

                if (game.Moves.Count > 0) {
                    AddMove(chessboard, m_Game.Moves.Last());
                    if (!m_Game.Settings.IsChess960)
                        UpdateEco();
                }
            }
            SetPlayerToMove();
        } // OnMoveMade

        public async void OnGameEnded(object sender, Chessboard.GameEndedEventArgs e)
        {
            // Save the game in the database
            if (m_Game.FullmoveNumber > 0)
                await m_Game.Save(Path.Join(App.GamesDatabasePath, $"{Guid.NewGuid().ToString("N")}.ccsf"));

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                OnResumeClick(null, new RoutedEventArgs());
                MessageWindow.CloseOpenedWindow();

                // Update the last move
                if ((e.Reason == Game.Results.Timeout || e.Reason == Game.Results.Resignation) && m_Game.Moves.Count > 0) {
                    var move = m_Game.Moves.Last();
                    var moves = this.FindControl<WrapPanel>("m_Moves");
                    var stack = moves.Children.Where(s => s.Name == $"move_{move.Index}").FirstOrDefault() as StackPanel;

                    IControl toRemove = stack.Children.Last();
                    if (toRemove != null) {
                        stack.Children.Remove(toRemove);
                        AddMove(m_Chessboard, move);
                    }
                }

                var dlg = new GameEndedWindow(m_Game);
                if (await dlg.ShowDialog<bool?>(this) == true) {
                    // Rematch
                    var settings = m_Game.Settings;
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
                await MessageWindow.ShowMessage(this, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["NoEnginesError"], MessageWindow.Icons.Error);
                return;
            }

            var ngw = new NewGameWindow();
            var newGame = await ngw.ShowDialog<NewGameWindow.Result>(this);

            if (newGame != null) {
                if (newGame.Color == null) {
                    var rnd = new Random().Next(2);
                    newGame.Color = rnd == 0 ? Game.Colors.White : Game.Colors.Black;
                }

                var game = new Game();
                var settings = new Game.GameSettings()
                {
                    IsChess960 = newGame.Chess960,
                    MaximumTime = newGame.MaximumTime,
                    TimeIncrement = newGame.TimeIncrement,
                    TrainingMode = newGame.TrainingMode,
                    MaxEngineThinkingTime = TimeSpan.FromSeconds(App.Settings.MaxEngineThinkingTimeSecs),
                    EngineDepth = App.Settings.MaxEngineDepth,
                    InitialFenPosition = newGame.InitialPosition,
                };

                settings.Players.Add(new HumanPlayer(newGame.Color.Value, App.Settings.PlayerName, null));

                var engine = App.Settings.GetEngine(newGame.EngineId)?.Copy();
                if (newGame.EngineElo.HasValue)
                    engine.SetElo(newGame.EngineElo.Value);
                var enginePlayer = new EnginePlayer(newGame.Color == Game.Colors.White ? Game.Colors.Black : Game.Colors.White,
                    newGame.TheKingPersonality?.DisplayName ?? engine.Name,
                    engine.GetElo());
                enginePlayer.Engine = engine;
                enginePlayer.Personality = newGame.Personality;
                enginePlayer.TheKingPersonality = newGame.TheKingPersonality;
                enginePlayer.OpeningBookFileName = App.Settings.OpeningBook;
                settings.Players.Add(enginePlayer);

                try {
                    game.Init(settings);
                } catch (Exception ) {
                    await MessageWindow.ShowMessage(this, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["InvalidFenString"], MessageWindow.Icons.Error);

                    settings.InitialFenPosition = string.Empty;
                    game.Init(settings);
                }
                await SetGame(game);
            }
        } // OnNewGameClick

        private async void OnSaveGameClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.DefaultExtension = "ccsf";
            dlg.Filters = new List<FileDialogFilter>()
            {
                new FileDialogFilter(){ Extensions = new List<string>() {"ccsf" }, Name = "CoreChess save file (*.ccsf)"},
                new FileDialogFilter(){ Extensions = new List<string>() {"pgn" }, Name = "Portable Game Notation (*.pgn)"},
            };
            string file = await dlg.ShowAsync(this);
            if (!string.IsNullOrEmpty(file)) {
                if (Path.GetExtension(file) == ".pgn")
                    await m_Game.SaveToPgn(file);
                else
                    await m_Game.Save(file);
            }
        } // OnSaveGameClick

        private async void OnLoadGameClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.AllowMultiple = false;
            dlg.Filters = new List<FileDialogFilter>()
            {
                new FileDialogFilter(){ Extensions = new List<string>() {"ccsf" }, Name = "CoreChess save file"},
                new FileDialogFilter(){ Extensions = new List<string>() {"pgn" }, Name = "Portable Game Notation (*.pgn)"},
            };
            string[] files = await dlg.ShowAsync(this);
            if (files?.Length > 0) {
                if (Path.GetExtension(files[0]) == ".pgn") {
                    var wDlg = new WaitWindow(Localizer.Localizer.Instance["LoadingPGN"]);
                    var wTask = wDlg.ShowDialog(this);
                    var games = await PGN.LoadFile(files[0]);
                    wDlg.Close();

                    Game game = null;
                    if (games.Count == 0)
                        await MessageWindow.ShowMessage(this, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["NoGameFoundInPgn"], MessageWindow.Icons.Error);
                    else if (games.Count == 1)
                        game = await Game.LoadFromPgn(games[0], true);
                    else {
                        var gDlg = new PgnGamesWindow(games);
                        var selGame = await gDlg.ShowDialog<PGN>(this);
                        if (selGame != null)
                            game = await Game.LoadFromPgn(selGame, false);
                    }

                    if (game != null) {
                        game.Settings.MaxEngineThinkingTime = TimeSpan.FromSeconds(App.Settings.MaxEngineThinkingTimeSecs);
                        game.Settings.EngineDepth = App.Settings.MaxEngineDepth;
                        await SetGame(game);
                    }
                } else {
                    try {
                        await SetGame(await Game.Load(files[0]));
                    } catch {
                        await MessageWindow.ShowMessage(this, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["LoadGameError"], MessageWindow.Icons.Error);
                    }
                }
            }
        } // OnLoadGameClick

        private async void OnResignClick(object sender, RoutedEventArgs e)
        {
            if (!m_Game.Ended && await MessageWindow.ShowConfirmMessage(this, Localizer.Localizer.Instance["Confirm"], Localizer.Localizer.Instance["ResignGameConfirm"]))
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
                            await Application.Current.Clipboard.SetTextAsync(pgn);
                            m_NotificationManager.Show(new Notification(Localizer.Localizer.Instance["Message"], Localizer.Localizer.Instance["PgnCopied"]));
                        } catch (Exception ex) {
                            await MessageWindow.ShowMessage(this, Localizer.Localizer.Instance["Error"], string.Format(Localizer.Localizer.Instance["ErrorCopyingString"], ex.Message), MessageWindow.Icons.Error);
                        }
                    }
                    File.Delete(tempFile);
                }
            }
        } // OnCopyPgnToClipboardClick

        private async void OnEngineSettingsClick(object sender, RoutedEventArgs e)
        {
            var dlg = new EngineSettingsWindow(m_Game);
            await dlg.ShowDialog(this);
        } // OnEngineSettingsClick

        private async void OnSaveToPngClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.Filters = new List<FileDialogFilter>()
            {
                new FileDialogFilter(){ Extensions = new List<string>() {"png" }, Name = "PNG image"}
            };
            string file = await dlg.ShowAsync(this);
            if (!string.IsNullOrEmpty(file)) {
                m_Chessboard.SaveToPng(file);
            }
        } // OnSaveToPngClick

        private async void OnCopyFenClick(object sender, RoutedEventArgs e)
        {
            try {
                await Application.Current.Clipboard.SetTextAsync(m_Game.GetFenString());
                m_NotificationManager.Show(new Notification(Localizer.Localizer.Instance["Message"], Localizer.Localizer.Instance["FenStringCopied"]));
            } catch (Exception ex){
                await MessageWindow.ShowMessage(this, Localizer.Localizer.Instance["Error"], string.Format(Localizer.Localizer.Instance["ErrorCopyingString"], ex.Message), MessageWindow.Icons.Error);
            }
        } // OnCopyFenClick

        private void OnFlipBoardClick(object sender, RoutedEventArgs e)
        {
            m_Chessboard.Flipped = !m_Chessboard.Flipped;
            m_Chessboard.Redraw();
        } // OnFlipBoardClick

        private async void OnGamesDatabaseClick(object sender, RoutedEventArgs e)
        {
            var wDlg = new WaitWindow(Localizer.Localizer.Instance["LoadingGames"]);
            var wTask = wDlg.ShowDialog(this);
            List<Game> games = new List<Game>();
            foreach (var f in Directory.GetFiles(App.GamesDatabasePath, "*.ccsf")) {
                var tempGame = await Game.Load(f);
                games.Add(tempGame);
            }
            wDlg.Close();
            var gDlg = new GamesDatabaseWindow(games);
            var selGame = await gDlg.ShowDialog<Game>(this);

            if (selGame != null) {
                await SetGame(selGame);
                DisplayMove(m_Game.Moves.Count - 1);
            }
        } // OnGamesDatabaseClick

        private async void OnEnginesClick(object sender, RoutedEventArgs e)
        {
            var dlg = new EnginesWindow();
            await dlg.ShowDialog(this);
        } // OnEnginesClick

        private async void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingsWindow();
            await dlg.ShowDialog(this);

            this.Topmost = App.Settings.Topmost;
            SetChessboardOptions();
            m_Chessboard.Redraw();

            if (m_Game != null) {
                UpdateCapturedPieces();
                await UpdateMoves();
            }
        } // OnSettingsClick

        private async void OnCheckForUpdatesClick(object sender, RoutedEventArgs e)
        {
            var updater = new Utils.AutoUpdater();
            if (await updater.CheckForUpdate(this, true)) {
                this.Close();
            }
        } // OnCheckForUpdatesClick

        private async void OnAboutClick(object sender, RoutedEventArgs e)
        {
            var dlg = new AboutWindow();
            await dlg.ShowDialog(this);
        } // OnAboutClick

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        } // OnExitClick
        #endregion

        #region Window events
        protected override void HandleWindowStateChanged(WindowState state)
        {
            if (state == WindowState.Minimized && App.Settings.AutoPauseWhenMinimized && m_Game?.Status == Game.Statuses.InProgress) {
                OnPauseClick(null, new RoutedEventArgs());
            }

            base.HandleWindowStateChanged(state);
        } // HandleWindowStateChanged

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.P) {
                e.Handled = true;
                if (m_Game.Status == Game.Statuses.InProgress)
                    OnPauseClick(null, new RoutedEventArgs());
                else if (m_Game.Status == Game.Statuses.Paused)
                    OnResumeClick(null, new RoutedEventArgs());
            }

            if (m_Context.IsPaused || e.Handled)
                return;

            var moveNav = this.FindControl<StackPanel>("m_MoveNavigator");
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
            } else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.F1) {
                e.Handled = true;
                OnAboutClick(null, new RoutedEventArgs());
            } else if (e.KeyModifiers == KeyModifiers.Alt && e.Key == Key.F4) {
                e.Handled = true;
                this.Close();
            } else if (moveNav.IsVisible && e.KeyModifiers == KeyModifiers.None && e.Key == Key.Left) {
                var btn = this.FindControl<Button>("m_MovePrevious");
                if (btn.IsEnabled) {
                    OnMoveNavigationClick(btn, new RoutedEventArgs());
                    e.Handled = true;
                }
            } else if (moveNav.IsVisible && e.KeyModifiers == KeyModifiers.None && e.Key == Key.Home) {
                var btn = this.FindControl<Button>("m_MoveFirst");
                if (btn.IsEnabled) {
                    OnMoveNavigationClick(btn, new RoutedEventArgs());
                    e.Handled = true;
                }
            } else if (moveNav.IsVisible && e.KeyModifiers == KeyModifiers.None && e.Key == Key.Right) {
                var btn = this.FindControl<Button>("m_MoveNext");
                if (btn.IsEnabled) {
                    OnMoveNavigationClick(btn, new RoutedEventArgs());
                    e.Handled = true;
                }
            } else if (moveNav.IsVisible && e.KeyModifiers == KeyModifiers.None && e.Key == Key.End) {
                var btn = this.FindControl<Button>("m_MoveLast");
                if (btn.IsEnabled) {
                    OnMoveNavigationClick(btn, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
        } // OnWindowKeyDown

        private async void OnWindowClosing(object sender, CancelEventArgs args)
        {
            if (m_Game != null) {
                args.Cancel = true;
                string autoSave = Path.Join(App.LocalPath, "autosave.ccsf");
                if (m_Game.Ended == false && m_Game.FullmoveNumber > 0 && App.Settings.AutoSaveGameOnExit) {
                    m_Game.WhiteTimeLeftMilliSecs = m_Game.LastWhiteTimeLeftMilliSecs;
                    m_Game.BlackTimeLeftMilliSecs = m_Game.LastBlackTimeLeftMilliSecs;
                    await m_Game.Save(autoSave);
                } else if (File.Exists(autoSave)) {
                    File.Delete(autoSave);
                }

                await m_Game.Stop();
                m_Game.Dispose();
                m_Game = null;
                this.Close();
            }
        } // OnWindowClosing
        #endregion

        #region Other events
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
        } // OnResumeClick

        private void OnMoveTapped(object sender, RoutedEventArgs e)
        {
            var move = (sender as TextBlock).DataContext as Game.MoveNotation;
            DisplayMove(m_Game.Moves.IndexOf(move));
        } // OnMoveTapped

        private async void OnMoveDoubleTapped(object sender, RoutedEventArgs e)
        {
            var move = (sender as TextBlock).DataContext as Game.MoveNotation;
            var dlg = new MoveCommentWindow(move);

            if (await dlg.ShowDialog<bool>(this)) {
                await UpdateMoves();
                if (!string.IsNullOrEmpty(m_Game.FileName)) {
                    try {
                        await m_Game.Save(m_Game.FileName);
                    } catch {
                    }
                }
            }
        } // OnMoveDoubleTapped

        private void OnMouseOnAnalysisResult(object sender, GameAnalyzeGraph.MouseEventArgs args)
        {
            var move = args.Index.HasValue ? m_Game.Moves[args.Index.Value - 1] : null;

            // Highlight the move
            var moves = this.FindControl<WrapPanel>("m_Moves");
            foreach (var child in moves.Children) {
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
                    DisplayMove(0);
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
            SetWaitAnimation(true);

            // Show engine output setting
            m_Context.ShowEngineOutput = App.Settings.ShowEngineOutput;

            Game game = null;

            // Check arguments
            if (m_args != null && m_args.Length > 0) {
                if (File.Exists(m_args[0])) {
                    try {
                        game = await Game.Load(m_args[0]);
                    } catch {
                    }
                }
            } else {
                // Check autosave
                string autoSave = Path.Join(App.LocalPath, "autosave.ccsf");
                if (File.Exists(autoSave)) {
                    try {
                        game = await Game.Load(autoSave);
                    } catch {
                    }
                    File.Delete(autoSave);
                }
            }

            if (game == null) {
                game = new Game();
                var settings = new Game.GameSettings()
                {
                    MaxEngineThinkingTime = TimeSpan.FromSeconds(App.Settings.MaxEngineThinkingTimeSecs),
                    EngineDepth = App.Settings.MaxEngineDepth,
                    MaximumTime = TimeSpan.FromMinutes(15)
                };
                settings.Players.Add(new HumanPlayer(Game.Colors.White, App.Settings.PlayerName, null));

                EngineBase lastUsedEngine = null;
                if (App.Settings.NewGame != null)
                    lastUsedEngine = App.Settings.GetEngine(App.Settings.NewGame.EngineId);
                if (lastUsedEngine == null)
                    lastUsedEngine = App.Settings.Engines?.FirstOrDefault();

                var enginePlayer = new EnginePlayer(Game.Colors.Black, lastUsedEngine?.Name, lastUsedEngine?.GetElo());
                var sSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects };
                enginePlayer.Engine = lastUsedEngine?.Copy();
                enginePlayer.OpeningBookFileName = App.Settings.OpeningBook;
                settings.Players.Add(enginePlayer);

                game.Init(settings);
            }

            await SetGame(game);

            // Check for update (Windows only)
            if (OperatingSystem.IsWindows()) {
                DispatcherTimer.RunOnce(async () =>
                {
                    var updater = new Utils.AutoUpdater();
                    if (await updater.CheckForUpdate(this)) {
                        this.Close();
                    }
                }, TimeSpan.FromSeconds(3), DispatcherPriority.Background);
            }
        } // InitializeWindow

        private void SetPlayerToMove()
        {
            var wBorder = this.FindControl<Border>("m_WhiteBorder");
            var bBorder = this.FindControl<Border>("m_BlackBorder");
            var wName = this.FindControl<TextBlock>("m_WhiteName");
            var bName = this.FindControl<TextBlock>("m_BlackName");
            if (m_Game.Ended) {
                wBorder.Classes.Remove("Selected");
                wName.Classes.Remove("HighlightColor");
                bBorder.Classes.Remove("Selected");
                bName.Classes.Remove("HighlightColor");
            } else if (m_Game.ToMove == Game.Colors.White) {
                wBorder.Classes.Add("Selected");
                wName.Classes.Add("HighlightColor");
                bBorder.Classes.Remove("Selected");
                bName.Classes.Remove("HighlightColor");
            } else {
                wBorder.Classes.Remove("Selected");
                wName.Classes.Remove("HighlightColor");
                bBorder.Classes.Add("Selected");
                bName.Classes.Add("HighlightColor");
            }
        } // SetPlayerToMove

        private void UpdateCapturedPieces()
        {
            if (m_Game == null)
                return;
            var wPieces = m_Game.CapturedPieces.Where(p => p.Color == Game.Colors.White).OrderByDescending(p => p.Value).ThenBy(p => p.Acronym).ToList();
            var bPieces = m_Game.CapturedPieces.Where(p => p.Color == Game.Colors.Black).OrderByDescending(p => p.Value).ThenBy(p => p.Acronym).ToList();
            int wValue = m_Game.Board.GetPieces(Game.Colors.White).Where(p => p.Type != Piece.Pieces.King).Sum(p => p.Value);
            int bValue = m_Game.Board.GetPieces(Game.Colors.Black).Where(p => p.Type != Piece.Pieces.King).Sum(p => p.Value);

            if (App.Settings.CapturedPieces == Settings.CapturedPiecesDisplay.Difference) {
                var tempWhite = new List<Piece>();
                var tempBlack = new List<Piece>();

                for (int i = 0; i < wPieces.Count; i++) {
                    var wp = wPieces[i];
                    var bp = bPieces.Where(p => p.Type == wp.Type).FirstOrDefault();
                    if (bp == null)
                        tempWhite.Add(wp);
                    else {
                        bPieces.Remove(bp);
                        wPieces.RemoveAt(i--);
                    }
                }

                for (int i = 0; i < bPieces.Count; i++) {
                    var bp  = bPieces[i];
                    var wp = wPieces.Where(p => p.Type == bp.Type).FirstOrDefault();
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

            var white = this.FindControl<WrapPanel>("m_WhiteCapturedPieces");
            white.Children.Clear();
            Piece lastPiece = null;
            foreach (var cp in bPieces) {
                string fileName = $"{m_Chessboard.PiecesFolder}{Path.DirectorySeparatorChar}b{cp.Type.ToString()}.png";
                var bitmap = new Bitmap(fileName);

                Thickness margin = new Thickness(0);
                if (lastPiece?.Type == cp.Type)
                    margin = new Thickness(-10,0,0,0);
                white.Children.Add(new Image() { Source = bitmap, Height = 35, Margin = margin });
                lastPiece = cp;
            }

            var black = this.FindControl<WrapPanel>("m_BlackCapturedPieces");
            black.Children.Clear();
            lastPiece = null;
            foreach (var cp in wPieces) {
                string fileName = $"{m_Chessboard.PiecesFolder}{Path.DirectorySeparatorChar}w{cp.Type.ToString()}.png";
                var bitmap = new Bitmap(fileName);

                Thickness margin = new Thickness(0);
                if (lastPiece?.Type == cp.Type)
                    margin = new Thickness(-10, 0, 0, 0);
                black.Children.Add(new Image() { Source = bitmap, Height = 35, Margin = margin });
                lastPiece = cp;
            }

            if (bValue > wValue) {
                black.Children.Add(new TextBlock()
                {
                    Text = $"+{bValue - wValue}",
                    Margin = new Thickness(5),
                    FontSize = 16,
                    FontWeight = FontWeight.Light,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                });
            } else if (wValue > bValue) {
                white.Children.Add(new TextBlock()
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
            var moves = this.FindControl<WrapPanel>("m_Moves");
            moves.Children.Clear();

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
                    tempBoard.ShowFileRankNotation = false;
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

        private void AddMove(Chessboard chessboard, Game.MoveNotation move, bool disposeGame = false)
        {
            var moves = this.FindControl<WrapPanel>("m_Moves");
            var stack = moves.Children.Where(s => s.Name == $"move_{move.Index}").FirstOrDefault() as StackPanel;
            if (stack == null) {
                stack = new StackPanel() { Name = $"move_{move.Index}", Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Thickness(0, 0, 5, 0) };
                moves.Children.Add(stack);
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
                var numberTxt = new TextBlock() { Text = $"{move.Index}.", Classes = new Classes(isFigurine ? "Figurine" : string.Empty), Margin = new Thickness(0, 2, 0, 2), MinWidth = 25, FontWeight = FontWeight.Regular };
                stack.Children.Add(numberTxt);
            }

            TextBlock moveTxt = new TextBlock() { DataContext = move, Classes = new Classes(classes), Text = $"{notation}{move.Annotation}", Margin = new Thickness(0, 2, 5, 2), MinWidth = 50, FontWeight = FontWeight.Light };
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

            DispatcherTimer.RunOnce(() =>
            {
                img.Source = chessboard.GetBitmap(new Size(300, 300), move);
                if (disposeGame)
                    chessboard.Game.Dispose();
            }, TimeSpan.FromMilliseconds(100), DispatcherPriority.Background);

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
        } // AddMove

        private void UpdateEco()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var m in m_Game.Moves) {
                sb.Append($"{m.ShortAlgebraic} ");
            }

            string ecoMoves = sb.ToString().Trim();
            if (m_Game.Moves.Count > 1 && !string.IsNullOrEmpty(ecoMoves)) {
                Eco foundEco;
                if (m_EcoDatabase.TryGetValue(ecoMoves, out foundEco)) {
                    m_Game.ECO = foundEco.Code;
                    if (string.IsNullOrEmpty(foundEco.Variation))
                        m_Context.EcoName = $"{foundEco.Code}: {foundEco.Name}";
                    else
                        m_Context.EcoName = $"{foundEco.Code}: {foundEco.Name}, {foundEco.Variation}";
                } else if (m_Game.Moves.Count > 10) {
                    m_Context.EcoName = string.Empty;
                }
            }
        } // UpdateEco

        private async Task<bool> SetGame(Game game)
        {
            var gameAnalysis = this.FindControl<GameAnalyzeGraph>("m_GameGraph");
            await gameAnalysis.Abort();
            gameAnalysis.Clear();

            if (App.Settings.Engines == null || App.Settings.Engines.Count == 0) {
                m_Chessboard.SetEmpty();
                SetChessboardOptions();
                SetWaitAnimation(false);
                await MessageWindow.ShowMessage(this, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["NoEnginesError"], MessageWindow.Icons.Error);
                return false;
            }

            SetWaitAnimation(true);
            await Task.Delay(1);

            if (m_Game != null) {
                await m_Game.Stop();
                m_Game.Dispose();
            }

            // Clear GUI:
            var wrap = this.FindControl<WrapPanel>("m_WhiteCapturedPieces");
            wrap.Children.Clear();
            wrap = this.FindControl<WrapPanel>("m_BlackCapturedPieces");
            wrap.Children.Clear();
            wrap = this.FindControl<WrapPanel>("m_Moves");
            wrap.Children.Clear();
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

            if (m_Game.Ended) {
                SetAnalyzeMode();
            } else {
                m_Context.IsResignEnabled = true;
                m_CurrentMoveIndex = null;
                this.FindControl<StackPanel>("m_MoveNavigator").IsVisible = false;
                this.FindControl<Border>("m_GameAnalyzeSection").IsVisible = false;
                this.FindControl<Border>("m_EngineMessageSection").IsVisible = App.Settings.ShowEngineOutput;
                this.FindControl<TextBlock>("m_WhiteTimeLeft").IsVisible = true;
                this.FindControl<TextBlock>("m_BlackTimeLeft").IsVisible = true;
                this.FindControl<Button>("m_PauseBtn").IsVisible = true;
            }

            m_Context.CanPause = !m_Game.Ended;
            m_Context.IsResignEnabled = m_Game.FullmoveNumber > 0;
            SetChessboardOptions();

            try {
                return await m_Chessboard.SetGame(m_Game);
            } catch (Exception ex) {
                SetWaitAnimation(false);
                m_Chessboard.SetEmpty();
                await MessageWindow.ShowMessage(this, Localizer.Localizer.Instance["Error"], string.Format(Localizer.Localizer.Instance["ErrorStartingEngine"], ex.Message, MessageWindow.Icons.Error));
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
            m_Chessboard.SquareWhiteColor = Utils.ColorConverter.ParseHexColor(App.Settings.WhiteColor);
            m_Chessboard.SquareWhiteSelectedColor = Utils.ColorConverter.ParseHexColor(App.Settings.WhiteSelectedColor);
            m_Chessboard.SquareBlackColor = Utils.ColorConverter.ParseHexColor(App.Settings.BlackColor);
            m_Chessboard.SquareBlackSelectedColor = Utils.ColorConverter.ParseHexColor(App.Settings.BlackSelectedColor);

            var border = this.FindControl<Border>("m_WhiteBorder");
            border.Background = new SolidColorBrush(Utils.ColorConverter.ParseHexColor(App.Settings.BlackColor));
            var image = this.FindControl<Image>("m_White");
            image.Source = new Bitmap(Path.Join(m_Chessboard.PiecesFolder, "wKing.png"));

            border = this.FindControl<Border>("m_BlackBorder");
            border.Background = new SolidColorBrush(Utils.ColorConverter.ParseHexColor(App.Settings.WhiteColor));
            image = this.FindControl<Image>("m_Black");
            image.Source = new Bitmap(Path.Join(m_Chessboard.PiecesFolder, "bKing.png"));
        } // SetChessboardOptions

        private void SetAnalyzeMode()
        {
            m_Context.IsResignEnabled = false;
            this.FindControl<Button>("m_PauseBtn").IsVisible = false;
            this.FindControl<TextBlock>("m_WhiteTimeLeft").IsVisible = false;
            this.FindControl<TextBlock>("m_BlackTimeLeft").IsVisible = false;
            this.FindControl<StackPanel>("m_MoveNavigator").IsVisible = true;

            this.FindControl<Button>("m_MoveFirst").IsEnabled = true;
            this.FindControl<Button>("m_MovePrevious").IsEnabled = true;
            this.FindControl<Button>("m_MoveNext").IsEnabled = false;
            this.FindControl<Button>("m_MoveLast").IsEnabled = false;

            m_CurrentMoveIndex = m_Game.Moves.Count - 1;
            this.FindControl<Border>("m_GameAnalyzeSection").IsVisible = true;
            this.FindControl<Border>("m_EngineMessageSection").IsVisible = false;

            var gameAnalysis = this.FindControl<GameAnalyzeGraph>("m_GameGraph");
            gameAnalysis.Clear();
            gameAnalysis.Game = m_Game;
            gameAnalysis.AnalyzeCompleted += async (s, args) => {
                // Save the analysis data
                if (!string.IsNullOrEmpty(gameAnalysis.Game.FileName)) {
                    try {
                        await gameAnalysis.Game.Save(gameAnalysis.Game.FileName);
                        DisplayMove(m_Game.Moves.Count - 1);
                    } catch {
                    }
                }
            };

            if (m_Game.AnalyzeResults == null) {
                if (App.Settings.GameAnalysisEngine == null)
                    gameAnalysis.IsVisible = false;
                else if (App.Settings.AutoAnalyzeGames)
                    gameAnalysis.Analyze(App.Settings.GameAnalysisEngine.GetDefaultAnalyzeDepth());
            } else {
                gameAnalysis.SetResults(m_Game.AnalyzeResults);
                DisplayMove(m_Game.Moves.Count - 1);
            }
        } // SetAnalyzeMode

        /// <summary>
        /// Display the given move on the chessboard
        /// </summary>
        /// <param name="moveIndex">The game move index</param>
        private bool DisplayMove(int moveIndex)
        {
            if (m_Game != null && m_Game.Ended && moveIndex >= 0 && moveIndex < m_Game.Moves.Count) {
                m_CurrentMoveIndex = moveIndex;
                var move = m_Game.Moves[moveIndex];
                m_Game.Board.InitFromFenString(move.Fen);
                m_Chessboard.Redraw(move);

                var gameAnalysis = this.FindControl<GameAnalyzeGraph>("m_GameGraph");
                gameAnalysis.AddMarker(moveIndex + 1);

                var moves = this.FindControl<WrapPanel>("m_Moves");
                foreach (var child in moves.Children) {
                    var stack = child as StackPanel;
                    foreach (var t in stack.Children) {
                        if (move != null && t.DataContext == move) {
                            t.Classes.Add("CurrentMove");
                            stack.BringIntoView();
                        } else
                            t.Classes.Remove("CurrentMove");
                    }
                }

                this.FindControl<Button>("m_MoveFirst").IsEnabled = moveIndex != 0;
                this.FindControl<Button>("m_MovePrevious").IsEnabled = moveIndex != 0;
                this.FindControl<Button>("m_MoveNext").IsEnabled = moveIndex < m_Game.Moves.Count - 1;
                this.FindControl<Button>("m_MoveLast").IsEnabled = moveIndex < m_Game.Moves.Count - 1;

                return true;
            }

            return false;
        } // DisplayMove

        private void SetWaitAnimation(bool visible)
        {
            m_Wait.IsVisible = visible;
            var stack = this.FindControl<StackPanel>("m_WaitSpinner");
            if (visible)
                stack.Classes.Add("spinner");
            else
                stack.Classes.Remove("spinner");
        } // SetWaitAnimation
        #endregion
    }
}
