using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using ChessLib;
using ChessLib.Engines;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreChess.Views
{
    public class NewGameWindow : BaseView
    {
        public class Result
        {
            public string EngineId { get; set; }
            public int? EngineElo { get; set; }
            public ChessLib.Game.Colors? Color { get; set; }
            public TimeSpan? MaximumTime { get; set; }
            public TimeSpan TimeIncrement { get; set; }
            public bool TrainingMode { get; set; }
            public bool Chess960 { get; set; }
            public string InitialPosition { get; set; }
            public TheKing.Personality Personality { get; set; }
        }

        public NewGameWindow()
        {
            this.InitializeComponent();
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();

            var white = this.FindControl<Image>("m_White");
            white.Source = new Bitmap(System.IO.Path.Combine(App.GetPiecesPath(App.Settings.PiecesSet), "wKnight.png"));
            var black = this.FindControl<Image>("m_Black");
            black.Source = new Bitmap(System.IO.Path.Combine(App.GetPiecesPath(App.Settings.PiecesSet), "bKnight.png"));

            var maxTime = this.FindControl<NumericUpDown>("m_MaxTime");
            maxTime.Value = 15;

            var engine = this.FindControl<ComboBox>("m_Engines");
            engine.Items = App.Settings.Engines.OrderBy(e => e.Name);
            engine.SelectedIndex = 0;

            var gType = this.FindControl<ComboBox>("m_GameType");
            gType.SelectedIndex = 0;

            // Check settings
            if (App.Settings.NewGame != null) {
                var whiteBtn = this.FindControl<ToggleButton>("m_WhiteBtn");
                var blackBtn = this.FindControl<ToggleButton>("m_BlackBtn");
                var randomBtn = this.FindControl<ToggleButton>("m_RandomBtn");
                var trainingMode = this.FindControl<ToggleSwitch>("m_TrainingMode");
                whiteBtn.IsChecked = App.Settings.NewGame.PlayerColor == Game.Colors.White;
                blackBtn.IsChecked = App.Settings.NewGame.PlayerColor == Game.Colors.Black;
                randomBtn.IsChecked = App.Settings.NewGame.PlayerColor == null;
                trainingMode.IsChecked = App.Settings.NewGame.TrainingMode == true;

                var selectedEngine = App.Settings.GetEngine(App.Settings.NewGame.EngineId);
                engine.SelectedItem = selectedEngine;
                if (engine.SelectedItem == null)
                    engine.SelectedIndex = 0;

                if (App.Settings.NewGame.Chess960)
                    gType.SelectedIndex = 1;

                if (App.Settings.NewGame.MaxTime.HasValue) {
                    maxTime.Value = App.Settings.NewGame.MaxTime.Value.TotalMinutes;
                }

                if (App.Settings.NewGame.Personality != null && selectedEngine is TheKing) {
                    var cmb = this.FindControl<ComboBox>("m_Personality");
                    cmb.SelectedItem = (cmb.Items as IEnumerable<TheKing.Personality>).Where(p => p.Name == App.Settings.NewGame.Personality.Name).FirstOrDefault();
                }

                var num = this.FindControl<NumericUpDown>("m_TimeIncrement");
                num.Value = App.Settings.NewGame.TimeIncrement.TotalSeconds;
            }
        }

        private void OnWhiteClick(object sender, RoutedEventArgs e)
        {
            (sender as ToggleButton).IsChecked = true;
            this.FindControl<ToggleButton>("m_BlackBtn").IsChecked = false;
            this.FindControl<ToggleButton>("m_RandomBtn").IsChecked = false;
        }

        private void OnBlackClick(object sender, RoutedEventArgs e)
        {
            (sender as ToggleButton).IsChecked = true;
            this.FindControl<ToggleButton>("m_WhiteBtn").IsChecked = false;
            this.FindControl<ToggleButton>("m_RandomBtn").IsChecked = false;
        }

        private void OnRandomClick(object sender, RoutedEventArgs e)
        {
            (sender as ToggleButton).IsChecked = true;
            this.FindControl<ToggleButton>("m_WhiteBtn").IsChecked = false;
            this.FindControl<ToggleButton>("m_BlackBtn").IsChecked = false;
        }

        private void OnEngineChanged(object sender, SelectionChangedEventArgs args)
        {
            var engine = ((ComboBox)sender).SelectedItem as EngineBase;
            if (engine == null)
                return;

            var gType = this.FindControl<ComboBox>("m_GameType");
            if (!engine.SupportChess960()) {
                gType.SelectedIndex = 0;
                this.FindControl<StackPanel>("m_GameTypeStack").IsVisible = false;
            } else {
                this.FindControl<StackPanel>("m_GameTypeStack").IsVisible = true;
            }

            var eloStack = this.FindControl<StackPanel>("m_EngineEloStack");
            eloStack.IsVisible = engine.CanSetElo();

            var elo = this.FindControl<NumericUpDown>("m_EngineElo");
            elo.Maximum = engine.GetMaxElo();
            elo.Minimum = engine.GetMinElo();
            elo.Value = elo.Maximum;

            // TheKing personalities
            if (engine is TheKing) {
                this.FindControl<StackPanel>("m_PersonalityStack").IsVisible = true;
                var cmb = this.FindControl<ComboBox>("m_Personality");
                var opt = engine.GetOption(TheKing.PersonalitiesFolderOptionName);
                if (opt != null && !string.IsNullOrEmpty(opt.Value))
                    cmb.Items = TheKing.Personality.GetFromFolder(opt.Value).OrderByDescending(p => p.Elo);
            } else {
                this.FindControl<StackPanel>("m_PersonalityStack").IsVisible = false;
            }

        } // OnEngineChanged

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            var engine = this.FindControl<ComboBox>("m_Engines");
            var engineElo = this.FindControl<NumericUpDown>("m_EngineElo");
            var initialPos = this.FindControl<TextBox>("m_FenString");
            var random = this.FindControl<ToggleButton>("m_RandomBtn");
            var white = this.FindControl<ToggleButton>("m_WhiteBtn");
            var maxTimeControl = this.FindControl<NumericUpDown>("m_MaxTime");
            var pers = this.FindControl<ComboBox>("m_Personality");
            var training = this.FindControl<ToggleSwitch>("m_TrainingMode");
            TimeSpan? maxTime = (TimeSpan?)TimeSpan.FromMinutes(maxTimeControl.Value);

            var num = this.FindControl<NumericUpDown>("m_TimeIncrement");

            var gameTypeCombo = this.FindControl<ComboBox>("m_GameType");

            // Save new game settings
            App.Settings.NewGame = new Settings.NewGameSettings()
            {
                EngineId = (engine.SelectedItem as EngineBase)?.Id,
                EngineElo = engineElo.IsVisible ? (int)engineElo.Value : null,
                PlayerColor = random.IsChecked.Value ? null : white.IsChecked.Value ? Game.Colors.White : Game.Colors.Black,
                MaxTime = maxTime,
                TrainingMode = training.IsChecked == true,
                TimeIncrement = TimeSpan.FromSeconds(num.Value),
                Chess960 = gameTypeCombo.SelectedIndex == 1,
                Personality = pers.SelectedItem as TheKing.Personality
            };
            App.Settings.Save(App.SettingsPath);

            this.Close(
                new Result()
                {
                    EngineId = (engine.SelectedItem as EngineBase)?.Id,
                    EngineElo = engineElo.IsVisible ? (int)engineElo.Value : null,
                    Color = random.IsChecked.Value ? null : white.IsChecked.Value ? Game.Colors.White : Game.Colors.Black,
                    MaximumTime = maxTime,
                    TimeIncrement = TimeSpan.FromSeconds(num.Value),
                    TrainingMode = training.IsChecked == true,
                    Chess960 = gameTypeCombo.SelectedIndex == 1,
                    InitialPosition = initialPos.Text?.Trim(),
                    Personality = pers.SelectedItem as TheKing.Personality
                }
            );
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
