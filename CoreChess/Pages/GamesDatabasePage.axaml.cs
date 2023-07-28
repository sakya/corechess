using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ChessLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoreChess.Abstracts;
using CoreChess.Dialogs;

namespace CoreChess.Pages
{
    public partial class GamesDatabaseWindow : BasePage
    {
        private readonly List<Game> m_Games;

        public GamesDatabaseWindow()
        {
            this.InitializeComponent();
        }

        public GamesDatabaseWindow(List<Game> games)
        {
            this.InitializeComponent();

            PageTitle = Localizer.Localizer.Instance["WT_GamesDatabaseWindow"];
            m_Games = games?.OrderByDescending(g => g.StartedTime).ToList();
            SetGamesList();
        }

        public Game SelectedGame { get; set; }

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
            SelectedGame = (sender as Controls.ItemsList).SelectedItem as Game;
            if (SelectedGame != null)
                await NavigateBack();
        } // OnListDoubleTapped

        private async void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (await MessageDialog.ShowConfirmMessage(App.MainWindow, Localizer.Localizer.Instance["Confirm"], Localizer.Localizer.Instance["RemoveGame"])) {
                var game = (sender as Button).DataContext as Game;
                File.Delete(game.FileName);

                int selectedIndex = m_Games.IndexOf(game);
                m_Games.Remove(game);
                SetGamesList(selectedIndex - 1);
            }
        } // OnRemoveClick

        private async void OnOkClick(object sender, RoutedEventArgs e)
        {
            SelectedGame = m_List.SelectedItem as Game;
            if (SelectedGame != null)
                await NavigateBack();
        }

        private async void OnCancelClick(object sender, RoutedEventArgs e)
        {
            await NavigateBack();
        }

        private void SetGamesList(int? selectedIndex = null)
        {
            m_List.Items = new List<Game>(m_Games);
            m_List.AttachedToVisualTree += (s, e) => m_List.Focus();
            UpdateInfoMessage();
            if (selectedIndex.HasValue && selectedIndex.Value > 0 && m_Games.Count > selectedIndex.Value)
                m_List.SelectedItem = m_Games[selectedIndex.Value];
        }

        private void FilterGames()
        {
            string filter = m_Search.Text;

            List<Game> filtered = null;

            if (string.IsNullOrWhiteSpace(filter))
                filtered = new List<Game>(m_Games);
            else
                filtered = m_Games.Where(g =>
                    g.GameTypeName.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                    g.Settings.WhitePlayerName != null && g.Settings.WhitePlayerName.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                    g.Settings.BlackPlayerName != null && g.Settings.BlackPlayerName.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

            m_List.Items = filtered;
            UpdateInfoMessage();
        }

        private void UpdateInfoMessage()
        {
            List<Game> items = m_List.Items as List<Game>;
            int total = items.Count;
            if (total > 0) {
                int win = items.Where(g => g.Winner == g.Settings.HumanPlayerColor).Count();
                int draw = items.Where(g => g.Result == Game.Results.Draw || g.Result == Game.Results.Stalemate).Count();
                int lost = items.Where(g => g.Winner != null && g.Winner != g.Settings.HumanPlayerColor).Count();

                m_Info.Text = string.Format(Localizer.Localizer.Instance["GameDatabaseInfo"],
                    total.ToString("###,##0", App.Settings.Culture),
                    $"{win.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)win / (double)total * 100, 2).ToString(App.Settings.Culture) }%)",
                    $"{draw.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)draw / (double)total * 100, 2).ToString(App.Settings.Culture) }%)",
                    $"{lost.ToString("###,##0", App.Settings.Culture)} ({ Math.Round((double)lost / (double)total * 100, 2).ToString(App.Settings.Culture) }%)");
            } else {
                m_Info.Text = string.Empty;
            }
        }
    }
}