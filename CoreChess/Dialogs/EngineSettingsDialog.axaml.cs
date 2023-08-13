using Avalonia.Interactivity;
using ChessLib;
using Avalonia.Media.Imaging;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public partial class EngineSettingsWindow : BaseDialog
    {
        public EngineSettingsWindow()
        {
            InitializeComponent();
        }

        public EngineSettingsWindow(Game game)
        {
            InitializeComponent();

            m_White.Source = new Bitmap(System.IO.Path.Combine(App.GetPiecesPath(App.Settings.PiecesSet), "wKnight.png"));
            m_Black.Source = new Bitmap(System.IO.Path.Combine(App.GetPiecesPath(App.Settings.PiecesSet), "bKnight.png"));

            var white = game.GetPlayer(Game.Colors.White);
            if (white is EnginePlayer wEngine) {
                m_WhiteName.Text = wEngine.Engine.Name;

                m_WhiteEngineOptions.IsVisible = true;
                m_WhiteEngineOptions.SetEngine(wEngine.Engine);
                m_WhiteEngineOptions.SetIsEnabled(false);
            } else {
                m_WhiteName.Text = white.Name;
                m_WhiteEngineOptions.IsVisible = false;
            }

            var black = game.GetPlayer(Game.Colors.Black);
            if (black is EnginePlayer bEngine) {
                m_BlackName.Text = bEngine.Engine.Name;

                m_BlackEngineOptions.IsVisible = true;
                m_BlackEngineOptions.SetEngine(bEngine.Engine);
                m_BlackEngineOptions.SetIsEnabled(false);
            } else {
                m_BlackName.Text = white.Name;
                m_BlackEngineOptions.IsVisible = false;
            }

            if (white is HumanPlayer && black is EnginePlayer)
                m_TabControl.SelectedIndex = 1;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}