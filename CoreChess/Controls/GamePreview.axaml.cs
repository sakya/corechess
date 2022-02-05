using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ChessLib;

namespace CoreChess.Controls
{
    public class GamePreview : UserControl
    {

        private static System.Threading.Semaphore m_Semaphore = new System.Threading.Semaphore(1, 3);
        private PGN m_PgnGame = null;
        private Game m_Game = null;

        public static readonly DirectProperty<GamePreview, PGN> PgnGameProperty =
                AvaloniaProperty.RegisterDirect<GamePreview, PGN>(
                    nameof(PgnGame),
                    o => o.PgnGame,
                    (o, v) => o.PgnGame = v);
        public static readonly DirectProperty<GamePreview, Game> GameProperty =
                AvaloniaProperty.RegisterDirect<GamePreview, Game>(
                    nameof(Game),
                    o => o.Game,
                    (o, v) => o.Game = v);

        public GamePreview()
        {
            this.InitializeComponent();
        }

        public PGN PgnGame {
            get { return m_PgnGame; }
            set {
                SetAndRaise(PgnGameProperty, ref m_PgnGame, value);

                this.FindControl<Image>("m_Image").Source = null;
                if (m_PgnGame != null)
                    DispatcherTimer.RunOnce(this.UpdateImage, TimeSpan.FromMilliseconds(100), DispatcherPriority.Background);
            }
        }

        public Game Game {
            get { return m_Game; }
            set {
                SetAndRaise(GameProperty, ref m_Game, value);

                this.FindControl<Image>("m_Image").Source = null;
                if (m_Game != null)
                    DispatcherTimer.RunOnce(this.UpdateImage, TimeSpan.FromMilliseconds(100), DispatcherPriority.Background);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void UpdateImage()
        {
            m_Semaphore.WaitOne();
            var img = this.FindControl<Image>("m_Image");

            if (Game != null) {
                await UpdateImageFromGame(Game);
            } else if (PgnGame == null) {
                img.Source = null;
            } else {
                try {
                    using (var game =  await Game.LoadFromPgn(PgnGame)) {
                        await UpdateImageFromGame(game);
                    }
                } catch {
                    img.Source = null;
                }
            }
            m_Semaphore.Release();
        } // UpdateImage

        private async Task<bool> UpdateImageFromGame(Game game)
        {
            var img = this.FindControl<Image>("m_Image");
            var chessboard = new Chessboard();
            chessboard.PiecesFolder = App.GetPiecesPath(App.Settings.PiecesSet);
            chessboard.ShowFileRankNotation = false;
            await chessboard.SetGame(game);

            img.Source = chessboard.GetBitmap(new Size(150, 150));

            return true;
        } // UpdateImageFromGame
    }
}