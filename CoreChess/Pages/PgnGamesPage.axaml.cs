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
    public partial class PgnGamesPage : BasePage
    {
        List<PGN> m_Games = null;

        public PgnGamesPage()
        {
            this.InitializeComponent();
        }

        public PgnGamesPage(List<PGN> games)
        {
            this.InitializeComponent();

            PageTitle = Localizer.Localizer.Instance["WT_PgnGamesWindow"];
            m_Games = games;
            m_List.Items = new List<PGN>(m_Games);
            m_List.AttachedToVisualTree += (s, e) => m_List.Focus();
            UpdateInfoMessage();
        }

        public PGN SelectedGame { get; set; }

        private async void OnOkClick(object sender, RoutedEventArgs e)
        {
            SelectedGame = m_List.SelectedItem as PGN;
            if (SelectedGame != null)
                await NavigateBack();
        }

        private async void OnCancelClick(object sender, RoutedEventArgs e)
        {
            await NavigateBack();
        }

        private void OnSearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) {
                e.Handled = true;
                FilterGames();
            } else if (e.Key == Key.Escape) {
                if (!string.IsNullOrEmpty(m_Search.Text)) {
                    e.Handled = true;
                    m_Search.Text = string.Empty;
                    FilterGames();
                }
            }
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            FilterGames();
        }

        private async void OnListDoubleTapped(object sender, TappedEventArgs e)
        {
            SelectedGame = (sender as Controls.ItemsList).SelectedItem as PGN;
            if (SelectedGame != null)
                await NavigateBack();
        } // OnListDoubleTapped

        private void FilterGames()
        {
            string filter = m_Search.Text;

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

            m_List.Items = filtered;
            UpdateInfoMessage();
        }

        private void UpdateInfoMessage()
        {
            List<PGN> items = m_List.Items as List<PGN>;

            int total = items.Count;
            if (total > 0) {
                int win = items.Where(g => g.Result == "1-0").Count();
                int draw = items.Where(g => g.Result == "1/2-1/2").Count();
                int lost = items.Where(g => g.Result == "0-1").Count();
                int other = items.Where(g => g.Result == "*").Count();
                m_Info.Text = string.Format(Localizer.Localizer.Instance["PgnGameInfo"],
                    total.ToString("###,##0", App.Settings.Culture),
                    $"{win.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)win / (double)total * 100, 2).ToString(App.Settings.Culture) }%)",
                    $"{draw.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)draw / (double)total * 100, 2).ToString(App.Settings.Culture) }%)",
                    $"{lost.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)lost / (double)total * 100, 2).ToString(App.Settings.Culture) }%)",
                    $"{other.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)other / (double)total * 100, 2).ToString(App.Settings.Culture) }%)");
            } else {
                m_Info.Text = string.Empty;
            }
        }
    }
}
