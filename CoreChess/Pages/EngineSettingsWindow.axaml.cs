using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChessLib;
using System.Linq;

namespace CoreChess.Views
{
    public class EngineSettingsWindow : BaseView
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
                var txt = this.FindControl<TextBlock>("m_Name");
                txt.Text = enginePlayer.Engine.Name;

                var ctrl = this.FindControl<Controls.EngineOptions>("m_EngineOptions");
                ctrl.SetEngine(enginePlayer.Engine);
                ctrl.SetIsEnabled(false);
            }
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}