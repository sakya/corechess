using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ChessLib;
using System.Threading.Tasks;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public partial class GameEndedDialog : BaseDialog
    {
        private readonly Game m_Game;

        public GameEndedDialog()
        {
            this.InitializeComponent();
        }

        public GameEndedDialog(Game game)
        {
            this.InitializeComponent();

            m_Game = game;
            if (m_Game.Winner != null)
                m_Image.Source = new Bitmap($"Images/Pieces/Default/{(m_Game.Winner == Game.Colors.White ? "w" : "b")}Knight.png");
            else
                m_Image.IsVisible = false;

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

            m_Message.Text = message;

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