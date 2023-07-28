using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChessLib;
using System.Linq;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public partial class EngineSettingsWindow : BaseDialog
    {
        public EngineSettingsWindow()
        {
            this.InitializeComponent();
        }

        public EngineSettingsWindow(Game game)
        {
            this.InitializeComponent();

            var enginePlayers = game.Settings.Players.Where(p => p is EnginePlayer).ToList();
            if (enginePlayers.Count == 1) {
                var enginePlayer = enginePlayers[0] as EnginePlayer;
                m_Name.Text = enginePlayer.Engine.Name;

                m_EngineOptions.SetEngine(enginePlayer.Engine);
                m_EngineOptions.SetIsEnabled(false);
            }
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}