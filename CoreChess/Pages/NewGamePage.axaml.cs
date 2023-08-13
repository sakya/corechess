using Avalonia.Input;
using Avalonia.Interactivity;
using ChessLib;
using System;
using System.Collections.Generic;
using CoreChess.Abstracts;
using CoreChess.Controls;

namespace CoreChess.Dialogs
{
    public partial class NewGamePage : BasePage
    {
        public class NewGameResult : Settings.NewGameSettings
        {
            public string InitialPosition { get; set; }
        }

        public NewGameResult Result { get; private set; }

        public NewGamePage()
        {
            this.InitializeComponent();

            PageTitle = Localizer.Localizer.Instance["WT_NewGameWindow"];

            m_Player1.Color = Game.Colors.White;
            m_Player1.IsHuman = true;
            m_Player1.PlayerName = App.Settings.PlayerName;

            m_Player2.IsHuman = false;
            m_Player2.PlayerName = App.Settings.PlayerName;


            m_MaxTime.Value = 15;
            m_TimeIncrement.Value = 0;
            m_GameType.SelectedIndex = 0;

            // Restore last new game settings
            if (App.Settings.NewGame != null) {
                if (App.Settings.NewGame.Players?.Count == 2) {
                    SetPlayer(m_Player1, App.Settings.NewGame.Players[0]);
                    SetPlayer(m_Player2, App.Settings.NewGame.Players[1]);
                }
                m_TrainingMode.IsChecked = App.Settings.NewGame.TrainingMode == true;

                if (App.Settings.NewGame.Chess960)
                    m_GameType.SelectedIndex = 1;

                if (App.Settings.NewGame.MaxTime.HasValue) {
                    m_MaxTime.Value = (decimal)App.Settings.NewGame.MaxTime.Value.TotalMinutes;
                }

                m_TimeIncrement.Value = (decimal)App.Settings.NewGame.TimeIncrement.TotalSeconds;
            }
        }

        private void SetPlayer(PlayerControl ctrl, Settings.NewGameSettings.Player player)
        {
            ctrl.IsHuman = player.IsHuman;
            ctrl.PlayerName = player.Name;
            ctrl.Color = player.Color;

            ctrl.EngineId = player.EngineId;
            ctrl.EngineElo = player.EngineElo;
            ctrl.EnginePersonality = player.Personality;
            ctrl.TheKingPersonality = player.TheKingPersonality;
            ctrl.OpeningBook = player.OpeningBook;
        }

        private Settings.NewGameSettings.Player GetPlayer(PlayerControl ctrl, Game.Colors? color = null)
        {
            return new Settings.NewGameSettings.Player()
            {
                Name = ctrl.PlayerName,
                Color = color ?? ctrl.Color,
                EngineId = ctrl.EngineId,
                EngineElo = ctrl.EngineElo,
                Personality = ctrl.EnginePersonality,
                TheKingPersonality = ctrl.TheKingPersonality,
                OpeningBook = ctrl.OpeningBook
            };
        }

        private async void OnOkClick(object sender, RoutedEventArgs e)
        {
            TimeSpan? maxTime = (TimeSpan?)TimeSpan.FromMinutes((double)m_MaxTime.Value);

            // Save new game settings
            App.Settings.NewGame = new Settings.NewGameSettings()
            {
                MaxTime = maxTime,
                TrainingMode = m_TrainingModeGrid.IsVisible && m_TrainingMode.IsChecked == true,
                TimeIncrement = TimeSpan.FromSeconds((double)m_TimeIncrement.Value),
                Chess960 = m_GameType.SelectedIndex == 1,
                Players = new List<Settings.NewGameSettings.Player>()
                {
                    GetPlayer(m_Player1),
                    GetPlayer(m_Player2)
                }
            };
            App.Settings.Save(App.SettingsPath);

            var rnd = new Random().Next(2);
            Result = new NewGameResult()
            {
                MaxTime = maxTime,
                TrainingMode = m_TrainingModeGrid.IsVisible && m_TrainingMode.IsChecked == true,
                TimeIncrement = TimeSpan.FromSeconds((double)m_TimeIncrement.Value),
                Chess960 = m_GameType.SelectedIndex == 1,
                Players = new List<Settings.NewGameSettings.Player>()
                {
                    GetPlayer(m_Player1, m_Player1.Color == null ? (rnd == 0 ? Game.Colors.White : Game.Colors.Black) : null),
                    GetPlayer(m_Player2, m_Player2.Color == null ? (rnd == 0 ? Game.Colors.Black : Game.Colors.White) : null),
                },
                InitialPosition = m_FenString.Text
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

        private void OnPlayer1ColorChanged(Game.Colors? color)
        {
            m_Player2.Color = color switch
            {
                Game.Colors.White => Game.Colors.Black,
                Game.Colors.Black => Game.Colors.White,
                _ => null
            };
        }

        private void OnPlayer2ColorChanged(Game.Colors? color)
        {
            m_Player1.Color = color switch
            {
                Game.Colors.White => Game.Colors.Black,
                Game.Colors.Black => Game.Colors.White,
                _ => null
            };
        }

        private void OnPlayerSupportChess960Changed(bool supported)
        {
            if (!m_Player1.SupportChess960 || !m_Player2.SupportChess960) {
                m_GameType.SelectedIndex = 0;
                m_GameTypeStack.IsVisible = false;
            } else {
                m_GameTypeStack.IsVisible = true;
            }
        }

        private void OnIsHumanChanged(bool isHuman)
        {
            m_TrainingModeGrid.IsVisible = m_Player1.IsHuman || m_Player2.IsHuman;
        }
    }
}
