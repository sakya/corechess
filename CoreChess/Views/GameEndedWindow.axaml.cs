using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ChessLib;
using System.Threading.Tasks;

namespace CoreChess.Views
{
    public class GameEndedWindow : BaseView
    {
        private Game m_Game = null;
        private Controls.GameAnalyzeGraph m_Graph = null;

        public GameEndedWindow()
        {
            this.InitializeComponent();
        }

        public GameEndedWindow(Game game)
        {
            this.InitializeComponent();

            m_Game = game;
            var img = this.FindControl<Image>("m_Image");
            if (m_Game.Winner != null)
                img.Source = new Bitmap($"Images/Pieces/Default/{(m_Game.Winner == Game.Colors.White ? "w" : "b")}Knight.png");
            else
                img.IsVisible = false;

            string message = string.Empty;
            if (m_Game.Result == Game.Results.Checkmate)
                message = string.Format(Localizer.Localizer.Instance["CheckmateMessage"], Localizer.Localizer.Instance[m_Game.Winner.ToString().ToLower()]);
            else if (m_Game.Result == Game.Results.Timeout)
                message = string.Format(Localizer.Localizer.Instance["TimeoutMessage"], Localizer.Localizer.Instance[m_Game.Winner.ToString().ToLower()]);
            else if (m_Game.Result == Game.Results.Stalemate)
                message = Localizer.Localizer.Instance["Stalemate"];
            else if (m_Game.Result == Game.Results.Draw)
                message = Localizer.Localizer.Instance["Draw"];
            else if (m_Game.Result == Game.Results.Resignation)
                message = string.Format(Localizer.Localizer.Instance["ResignationMessage"], Localizer.Localizer.Instance[m_Game.Winner.ToString().ToLower()]);

            var txt = this.FindControl<TextBlock>("m_Message");
            txt.Text = message;

            m_Graph = this.FindControl<Controls.GameAnalyzeGraph>("m_Graph");
            if (App.Settings.GameAnalysisEngine == null) {
                m_Graph.IsVisible = false;
            } else {
                m_Graph.AttachedToVisualTree += async (sender, args) => {
                    m_Graph.Clear();
                    m_Graph.Game = m_Game;
                    m_Graph.AnalyzeCompleted += async (s, args) => {
                        // Save the analysis data
                        if (!string.IsNullOrEmpty(m_Graph.Game.FileName))
                            await m_Graph.Game.Save(m_Graph.Game.FileName);
                    };

                    if (App.Settings.AutoAnalyzeGames) {
                        await Task.Delay(100);
                        m_Graph.Analyze(App.Settings.GameAnalysisEngine.GetDefaultAnalyzeDepth());
                    }
                };
            }
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();
        }

        private async void OnRematchClick(object sender, RoutedEventArgs e)
        {
            await m_Graph.Abort();
            this.Close(true);
        }

        private async void OnCloseClick(object sender, RoutedEventArgs e)
        {
            await m_Graph.Abort();
            this.Close(false);
        }
    }
}