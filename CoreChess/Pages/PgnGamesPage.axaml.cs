using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChessLib;
using System;
using System.Collections.Generic;
using System.Linq;
using CoreChess.Abstracts;

namespace CoreChess.Pages
{
    public class PgnGamesPage : BasePage
    {
        List<PGN> m_Games = null;

        public PgnGamesPage()
        {
            this.InitializeComponent();
        }

        public PgnGamesPage(List<PGN> games)
        {
            this.InitializeComponent();

            m_Games = games;
            var list = this.FindControl<Controls.ItemsList>("m_List");
            list.Items = new List<PGN>(m_Games);
            list.AttachedToVisualTree += (s, e) => list.Focus();
            UpdateInfoMessage();
        }

        public PGN SelectedGame { get; set; }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnOkClick(object sender, RoutedEventArgs e)
        {
            var list = this.FindControl<Controls.ItemsList>("m_List");

            SelectedGame = list.SelectedItem as PGN;
            if (SelectedGame != null)
                await NavigateBack();
        }

        private async void OnCancelClick(object sender, RoutedEventArgs e)
        {
            await NavigateBack();
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

        private async void OnListDoubleTapped(object sender, RoutedEventArgs e)
        {
            SelectedGame = (sender as Controls.ItemsList).SelectedItem as PGN;
            if (SelectedGame != null)
                await NavigateBack();
        } // OnListDoubleTapped

        private void FilterGames()
        {
            var txt = this.FindControl<TextBox>("m_Search");
            string filter = txt.Text;

            List<PGN> filtered = null;

            if (string.IsNullOrWhiteSpace(filter))
                filtered = new List<PGN>(m_Games);
            else
                filtered = m_Games.Where(g =>
                    g.Event != null && g.Event.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                    g.Site != null && g.Site.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                    g.White != null && g.White.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                    g.Black != null && g.Black.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

            var list = this.FindControl<Controls.ItemsList>("m_List");
            list.Items = filtered;
            UpdateInfoMessage();
        }

        private void UpdateInfoMessage()
        {
            var list = this.FindControl<Controls.ItemsList>("m_List");
            List<PGN> items = list.Items as List<PGN>;

            int total = items.Count;
            if (total > 0) {
                int win = items.Where(g => g.Result == "1-0").Count();
                int draw = items.Where(g => g.Result == "1/2-1/2").Count();
                int lost = items.Where(g => g.Result == "0-1").Count();
                int other = items.Where(g => g.Result == "*").Count();
                this.FindControl<TextBlock>("m_Info").Text = string.Format(Localizer.Localizer.Instance["PgnGameInfo"],
                    total.ToString("###,##0", App.Settings.Culture),
                    $"{win.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)win / (double)total * 100, 2).ToString(App.Settings.Culture) }%)",
                    $"{draw.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)draw / (double)total * 100, 2).ToString(App.Settings.Culture) }%)",
                    $"{lost.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)lost / (double)total * 100, 2).ToString(App.Settings.Culture) }%)",
                    $"{other.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)other / (double)total * 100, 2).ToString(App.Settings.Culture) }%)");
            } else {
                this.FindControl<TextBlock>("m_Info").Text = string.Empty;
            }
        }
    }
}
