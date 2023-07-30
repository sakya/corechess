using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ChessLib;
using ChessLib.Engines;
using System;
using System.Collections.Generic;
using System.Linq;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public partial class NewGamePage : BasePage
    {
        public class NewGameResult
        {
            public string EngineId { get; set; }
            public int? EngineElo { get; set; }
            public ChessLib.Game.Colors? Color { get; set; }
            public TimeSpan? MaximumTime { get; set; }
            public TimeSpan TimeIncrement { get; set; }
            public bool TrainingMode { get; set; }
            public bool Chess960 { get; set; }
            public string InitialPosition { get; set; }
            public string Personality { get; set; }
            public TheKing.Personality TheKingPersonality { get; set; }
        }

        public NewGameResult Result { get; private set; }

        public NewGamePage()
        {
            this.InitializeComponent();

            PageTitle = Localizer.Localizer.Instance["WT_NewGameWindow"];
            m_White.Source = new Bitmap(System.IO.Path.Combine(App.GetPiecesPath(App.Settings.PiecesSet), "wKnight.png"));
            m_Black.Source = new Bitmap(System.IO.Path.Combine(App.GetPiecesPath(App.Settings.PiecesSet), "bKnight.png"));

            m_MaxTime.Value = 15;
            m_Engines.ItemsSource = App.Settings.Engines.OrderBy(e => e.Name);
            m_Engines.SelectedIndex = 0;

            m_GameType.SelectedIndex = 0;

            // Restore last new game settings
            if (App.Settings.NewGame != null) {
                m_WhiteBtn.IsChecked = App.Settings.NewGame.PlayerColor == Game.Colors.White;
                m_BlackBtn.IsChecked = App.Settings.NewGame.PlayerColor == Game.Colors.Black;
                m_RandomBtn.IsChecked = App.Settings.NewGame.PlayerColor == null;
                m_TrainingMode.IsChecked = App.Settings.NewGame.TrainingMode == true;

                var selectedEngine = App.Settings.GetEngine(App.Settings.NewGame.EngineId);
                m_Engines.SelectedItem = selectedEngine;
                if (m_Engines.SelectedItem == null)
                    m_Engines.SelectedIndex = 0;

                if (App.Settings.NewGame.EngineElo.HasValue) {
                    m_EngineElo.Value = App.Settings.NewGame.EngineElo.Value;
                }

                if (App.Settings.NewGame.Chess960)
                    m_GameType.SelectedIndex = 1;

                if (App.Settings.NewGame.MaxTime.HasValue) {
                    m_MaxTime.Value = (decimal)App.Settings.NewGame.MaxTime.Value.TotalMinutes;
                }

                if (App.Settings.NewGame.TheKingPersonality != null && selectedEngine is TheKing) {
                    m_TheKingPersonality.SelectedItem = (m_TheKingPersonality.Items as IEnumerable<TheKing.Personality>).FirstOrDefault(p => p.Name == App.Settings.NewGame.TheKingPersonality.Name);
                } else if (!string.IsNullOrEmpty(App.Settings.NewGame.Personality)) {
                    m_Personality.SelectedItem = App.Settings.NewGame.Personality;
                }

                m_TimeIncrement.Value = (decimal)App.Settings.NewGame.TimeIncrement.TotalSeconds;
            }
        }

        private void OnWhiteClick(object sender, RoutedEventArgs e)
        {
            (sender as ToggleButton).IsChecked = true;
            m_BlackBtn.IsChecked = false;
            m_RandomBtn.IsChecked = false;
        }

        private void OnBlackClick(object sender, RoutedEventArgs e)
        {
            (sender as ToggleButton).IsChecked = true;
            m_WhiteBtn.IsChecked = false;
            m_RandomBtn.IsChecked = false;
        }

        private void OnRandomClick(object sender, RoutedEventArgs e)
        {
            (sender as ToggleButton).IsChecked = true;
            m_WhiteBtn.IsChecked = false;
            m_BlackBtn.IsChecked = false;
        }

        private void OnEngineChanged(object sender, SelectionChangedEventArgs args)
        {
            var engine = ((ComboBox)sender).SelectedItem as EngineBase;
            if (engine == null)
                return;

            if (!engine.SupportChess960()) {
                m_GameType.SelectedIndex = 0;
                m_GameTypeStack.IsVisible = false;
            } else {
                m_GameTypeStack.IsVisible = true;
            }

            m_EngineEloStack.IsVisible = engine.CanSetElo();

            m_EngineElo.Maximum = engine.GetMaxElo();
            m_EngineElo.Minimum = engine.GetMinElo();
            m_EngineElo.Value = m_EngineElo.Maximum;

            // TheKing personalities
            if (engine is TheKing) {
                m_TheKingPersonalityStack.IsVisible = true;
                var opt = engine.GetOption(TheKing.PersonalitiesFolderOptionName);
                if (opt != null && !string.IsNullOrEmpty(opt.Value))
                    m_TheKingPersonality.ItemsSource = TheKing.Personality.GetFromFolder(opt.Value).OrderByDescending(p => p.Elo);
            } else {
                m_TheKingPersonalityStack.IsVisible = false;

                if (engine is Uci) {
                    // Dragon 2.6 personality
                    var pOpt = engine.GetOption(Uci.PersonalityOptionNames);
                    if (pOpt != null && pOpt.Type == "combo") {
                        m_PersonalityStack.IsVisible = true;
                        m_Personality.ItemsSource = pOpt.ValidValues;
                        m_Personality.SelectedItem = pOpt.Default;
                    } else {
                        m_PersonalityStack.IsVisible = false;
                    }
                }
            }
        } // OnEngineChanged

        private async void OnOkClick(object sender, RoutedEventArgs e)
        {
            TimeSpan? maxTime = (TimeSpan?)TimeSpan.FromMinutes((double)m_MaxTime.Value);

            // Save new game settings
            App.Settings.NewGame = new Settings.NewGameSettings()
            {
                EngineId = (m_Engines.SelectedItem as EngineBase)?.Id,
                EngineElo = m_EngineElo.IsVisible ? (int)m_EngineElo.Value : null,
                PlayerColor = m_RandomBtn.IsChecked.Value ? null : m_WhiteBtn.IsChecked.Value ? Game.Colors.White : Game.Colors.Black,
                MaxTime = maxTime,
                TrainingMode = m_TrainingMode.IsChecked == true,
                TimeIncrement = TimeSpan.FromSeconds((double)m_TimeIncrement.Value),
                Chess960 = m_GameType.SelectedIndex == 1,
                Personality = m_Personality.SelectedItem as string,
                TheKingPersonality = m_TheKingPersonality.SelectedItem as TheKing.Personality
            };
            App.Settings.Save(App.SettingsPath);

            Result = new NewGameResult()
            {
                EngineId = (m_Engines.SelectedItem as EngineBase)?.Id,
                EngineElo = m_EngineElo.IsVisible ? (int)m_EngineElo.Value : null,
                Color = m_RandomBtn.IsChecked.Value ? null :
                    m_WhiteBtn.IsChecked.Value ? Game.Colors.White : Game.Colors.Black,
                MaximumTime = maxTime,
                TimeIncrement = TimeSpan.FromSeconds((double)m_TimeIncrement.Value),
                TrainingMode = m_TrainingMode.IsChecked == true,
                Chess960 = m_GameType.SelectedIndex == 1,
                InitialPosition = m_FenString.Text?.Trim(),
                Personality = m_Personality.SelectedItem as string,
                TheKingPersonality = m_TheKingPersonality.SelectedItem as TheKing.Personality
            };

            await NavigateBack();
        } // OnOkClick

        private async void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Result = null;
            await NavigateBack();
        } // OnCancelClick

        public override void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Escape) {
                e.Handled = true;
                OnCancelClick(null, null);
            }
        }
    }
}
