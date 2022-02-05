using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChessLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreChess.Views
{
    public class GamesDatabaseWindow : BaseView
    {
        List<Game> m_Games = null;

        public GamesDatabaseWindow()
        {
            this.InitializeComponent();
        }

        public GamesDatabaseWindow(List<Game> games)
        {
            this.InitializeComponent();

            m_Games = games;
            var list = this.FindControl<Controls.ItemsList>("m_List");
            list.Items = new List<Game>(m_Games.OrderByDescending(g => g.StartedTime));
            UpdateInfoMessage();
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) {
                e.Handled = true;
                FilterGames();
            } else if (e.Key == Key.Escape) {
                var txt = this.FindControl<TextBox>("m_Search");
                if (!string.IsNullOrEmpty(txt.Text)) {
                    e.Handled = true;
                    txt.Text = string.Empty;
                    FilterGames();
                }
            }
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            FilterGames();
        }

        private void OnListDoubleTapped(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Controls.ItemsList).SelectedItem as Game;
            if (selected != null)
                this.Close(selected);
        } // OnListDoubleTapped

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            var list = this.FindControl<Controls.ItemsList>("m_List");

            var selected = list.SelectedItem as Game;
            if (selected != null)
                this.Close(selected);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.Close(null);
        }

        private void FilterGames()
        {
            var txt = this.FindControl<TextBox>("m_Search");
            string filter = txt.Text;

            List<Game> filtered = null;

            if (string.IsNullOrWhiteSpace(filter))
                filtered = new List<Game>(m_Games);
            else
                filtered = m_Games.Where(g =>
                    g.GameType.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                    g.Settings.WhitePlayerName != null && g.Settings.WhitePlayerName.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                    g.Settings.BlackPlayerName != null && g.Settings.BlackPlayerName.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

            var list = this.FindControl<Controls.ItemsList>("m_List");
            list.Items = filtered;
            UpdateInfoMessage();
        }

        private void UpdateInfoMessage()
        {
            var list = this.FindControl<Controls.ItemsList>("m_List");
            List<Game> items = list.Items as List<Game>;

            int total = items.Count;
            if (total > 0) {
                int win = items.Where(g => g.Winner == g.Settings.HumanPlayerColor).Count();
                int draw = items.Where(g => g.Result == Game.Results.Draw || g.Result == Game.Results.Stalemate).Count();
                int lost = items.Where(g => g.Winner != null && g.Winner != g.Settings.HumanPlayerColor).Count();

                this.FindControl<TextBlock>("m_Info").Text = string.Format(Localizer.Localizer.Instance["GameDatabaseInfo"],
                    total.ToString("###,##0", App.Settings.Culture),
                    $"{win.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)win / (double)total * 100, 2).ToString(App.Settings.Culture) }%)",
                    $"{draw.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)draw / (double)total * 100, 2).ToString(App.Settings.Culture) }%)",
                    $"{lost.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)lost / (double)total * 100, 2).ToString(App.Settings.Culture) }%)");
            } else {
                this.FindControl<TextBlock>("m_Info").Text = string.Empty;
            }
        }
    }
}