using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ChessLib.Engines;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform.Storage;
using CoreChess.Abstracts;

namespace CoreChess.Pages
{
    public partial class SettingsPage : BasePage
    {
        public class PiecesSet
        {
            public string Name { get; set; }
            public string SamplePath {get; set; }
        } // PiecesSet

        public class Style
        {
            public Style(Settings.Styles style, string name)
            {
                Value = style;
                Name = name;
            }

            public string Name { get; set; }
            public Settings.Styles Value { get; set; }
        } // Style

        public class Language
        {
            public Language(string code, string name)
            {
                Code = code;
                Name = name;
            }

            public string Name { get; set; }
            public string Code { get; set; }
        } // Language

        public class ColorTheme
        {
            public ColorTheme(string name, string whiteColor, string whiteSelectedColor, string blackColor, string blackSelectedColor)
            {
                Name = name;
                WhiteColor = whiteColor;
                WhiteSelectedColor = whiteSelectedColor;
                BlackColor = blackColor;
                BlackSelectedColor = blackSelectedColor;
            }

            public string Name { get; set; }
            public string WhiteColor { get; set; }
            public string WhiteSelectedColor { get; set; }
            public string BlackColor { get; set; }
            public string BlackSelectedColor { get; set; }
        } // ColorTheme

        private List<Style> m_StylesList = new List<Style>()
        {
            new Style(Settings.Styles.Light, Localizer.Localizer.Instance["StyleLight"]),
            new Style(Settings.Styles.Dark, Localizer.Localizer.Instance["StyleDark"])
        };

        private List<Language> m_SupportedLanguages = new List<Language>()
        {
            new Language("en-US", "English"),
            new Language("it-IT", "Italian"),
        };

        private List<ColorTheme> m_ColorThemes = new List<ColorTheme>()
        {
            new ColorTheme("Custom", string.Empty, string.Empty, string.Empty, string.Empty),
            new ColorTheme("Default", "#ffeeeed2", "#fff7f783", "#ff769656", "#ffbbcb44"),
            new ColorTheme("Uscf", "#ffc3c6be", "#ffffffff", "#ff727fa2", "#ff465e9e"),
            new ColorTheme("Wikipedia", "#ffffce9e", "#ffffe3c8", "#ffd18b47", "#ffd17115"),
            new ColorTheme("Chess24", "#ff9e7863", "#ff9e8d83", "#ff633526", "#ff642612"),
            new ColorTheme("Leipzig", "#ffffffff", "#fff7f783", "#ffe1e1e1", "#ffbbcb44"),
            new ColorTheme("Symbol", "#ffffffff", "#ffc3c6be", "#ff58ac8a", "#ff18ab70"),
        };

        public SettingsPage()
        {
            InitializeComponent();
            Init();
        }

        public bool? Result { get; private set; }

        private void Init()
        {
            PageTitle = Localizer.Localizer.Instance["WT_SettingsWindow"];
            var fonts = SkiaSharp.SKFontManager.Default.FontFamilies.OrderBy(f => f).ToList();
            fonts.Insert(0, "Default (Roboto)");
            m_Fonts.ItemsSource = fonts;
            m_Fonts.SelectedItem = fonts.FirstOrDefault(f => f == App.Settings.FontFamily) ?? fonts[0];

            m_AccentButton.Color = Utils.ColorConverter.ParseHexColor(App.Settings.AccentColor);
            m_AccentButton.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == ColorView.ColorProperty.Name)
                    Application.Current.Resources["SystemAccentColor"] = m_AccentButton.Color;
            };

            m_HighlightButton.Color = Utils.ColorConverter.ParseHexColor(App.Settings.HighlightColor);
            m_HighlightButton.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == ColorView.ColorProperty.Name)
                    Application.Current.Resources["HighlightColor"] = m_HighlightButton.Color;
            };

            m_RestoreWindowSizeAndPosition.IsChecked = App.Settings.RestoreWindowSizeAndPosition;

            m_WhiteButton.Color = Utils.ColorConverter.ParseHexColor(App.Settings.WhiteColor);
            m_WhiteSelectedButton.Color = Utils.ColorConverter.ParseHexColor(App.Settings.WhiteSelectedColor);
            m_BlackButton.Color = Utils.ColorConverter.ParseHexColor(App.Settings.BlackColor);
            m_BlackSelectedButton.Color = Utils.ColorConverter.ParseHexColor(App.Settings.BlackSelectedColor);

            m_PlayerName.Text = App.Settings.PlayerName;

            m_Styles.ItemsSource = m_StylesList;
            m_Styles.SelectedItem = m_StylesList.FirstOrDefault(l => l.Value == App.Settings.Style);

            m_Languages.ItemsSource = m_SupportedLanguages;
            m_Languages.SelectedItem = m_SupportedLanguages.FirstOrDefault(l => l.Code == Localizer.Localizer.Instance.Language);

            m_EnableDragAndDrop.IsChecked = App.Settings.EnableDragAndDrop;
            m_ShowValidMoves.IsChecked = App.Settings.ShowValidMoves;
            m_ShowFileRankNotation.SelectedIndex = (int)App.Settings.ShowFileRankNotation;
            m_ShowSquareAttackIndicators.IsChecked = App.Settings.ShowSquareAttackIndicators;
            m_AttackColorMode.SelectedIndex = (int)App.Settings.AttackColorMode;

            m_Topmost.IsChecked = App.Settings.Topmost;
            m_EnableAudio.IsChecked = App.Settings.EnableAudio;
            m_SaveOnExit.IsChecked = App.Settings.AutoSaveGameOnExit;
            m_AutoAnalyzeGames.IsChecked = App.Settings.AutoAnalyzeGames;
            m_AutoPauseWhenMinimized.IsChecked = App.Settings.AutoPauseWhenMinimized;

            m_AnalysisEngines.ItemsSource = App.Settings.Engines.OrderBy(e => e.Name);
            if (!string.IsNullOrEmpty(App.Settings.GameAnalysisEngineId))
                m_AnalysisEngines.SelectedItem = App.Settings.GetEngine(App.Settings.GameAnalysisEngineId);

            m_MaxEngineThinkingTimeSecs.Value = App.Settings.MaxEngineThinkingTimeSecs;
            m_MaxEngineDepth.Value = App.Settings.MaxEngineDepth ?? 0;

            m_OpeningBook.Value = App.Settings.DefaultOpeningBook;

            // Color theme
            m_ColorTheme.ItemsSource = m_ColorThemes;
            m_ColorTheme.SelectedItem = m_ColorThemes
                .FirstOrDefault(ct => ct.WhiteColor == App.Settings.WhiteColor &&
                                      ct.WhiteSelectedColor == App.Settings.WhiteSelectedColor &&
                                      ct.BlackColor == App.Settings.BlackColor &&
                                      ct.BlackSelectedColor == App.Settings.BlackSelectedColor);
            if (m_ColorTheme.SelectedItem == null) {
                // Custom color theme
                m_ColorTheme.SelectedIndex = 0;

                m_WhiteButton.Color = Utils.ColorConverter.ParseHexColor(App.Settings.WhiteColor);
                m_WhiteSelectedButton.Color = Utils.ColorConverter.ParseHexColor(App.Settings.WhiteSelectedColor);

                m_BlackButton.Color = Utils.ColorConverter.ParseHexColor(App.Settings.BlackColor);
                m_BlackSelectedButton.Color = Utils.ColorConverter.ParseHexColor(App.Settings.BlackSelectedColor);
            }

            // Check pieces set
            PiecesSet sel = null;
            List<PiecesSet> sets = new List<PiecesSet>();

            List<string> piecesFolders = new List<string>()
            {
                App.LocalPiecesPath,
                App.PiecesPath
            };

            foreach (var pp in piecesFolders) {
                if (System.IO.Directory.Exists(pp)) {
                    foreach (var d in System.IO.Directory.GetDirectories(pp)) {
                        var ps = new PiecesSet()
                        {
                            Name = d.Remove(0, pp.Length + 1),
                            SamplePath = System.IO.Path.Join(d, "wKnight.png")
                        };
                        if (sets.FirstOrDefault(s => s.Name == ps.Name) == null) {
                            sets.Add(ps);

                            if (ps.Name == App.Settings.PiecesSet)
                                sel = ps;
                        }
                    }
                }
            }

            sets = sets.OrderBy(s => s.Name).ToList();
            m_PiecesSet.ItemsSource = sets;
            m_PiecesSet.SelectedItem = sel;
        }

        private void OnStyleChanged(object sender, SelectionChangedEventArgs args)
        {
            var cb = sender as ComboBox;
            var style = (Style)cb.SelectedItem;
            if (App.CurrentStyle != style.Value)
                App.SetStyle(style.Value, Utils.ColorConverter.ToHex(m_AccentButton.Color), Utils.ColorConverter.ToHex(m_HighlightButton.Color),
                             string.Empty);
        } // OnStyleChanged

        private void OnFontChanged(object sender, SelectionChangedEventArgs args)
        {
            var cb = sender as ComboBox;
            var font = cb.SelectedIndex == 0 ? Settings.DefaultFontFamily : (string)cb.SelectedItem;
            Application.Current.Resources["DefaultFontFamily"] = new Avalonia.Media.FontFamily(font);
        }

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs args)
        {
            var cb = sender as ComboBox;
            var language = (Language)cb.SelectedItem;
            Localizer.Localizer.Instance.LoadLanguage(language.Code);
        } // OnLanguageChanged

        private void OnColorThemeChanged(object sender, SelectionChangedEventArgs args)
        {
            var cb = sender as ComboBox;
            var ct = cb.SelectedItem as ColorTheme;
            bool isCustom = string.IsNullOrEmpty(ct.WhiteColor);

            m_WhiteButton.Color = Utils.ColorConverter.ParseHexColor(ct.WhiteColor);
            m_WhiteButton.IsEnabled = isCustom;
            m_WhiteSelectedButton.Color = Utils.ColorConverter.ParseHexColor(ct.WhiteSelectedColor);
            m_WhiteSelectedButton.IsEnabled = isCustom;
            m_BlackButton.Color = Utils.ColorConverter.ParseHexColor(ct.BlackColor);
            m_BlackButton.IsEnabled = isCustom;
            m_BlackSelectedButton.Color = Utils.ColorConverter.ParseHexColor(ct.BlackSelectedColor);
            m_BlackSelectedButton.IsEnabled = isCustom;
        } // OnColorThemeChanged

        private async void OnOkClick(object sender, RoutedEventArgs e)
        {
            App.Settings.PlayerName = m_PlayerName.Text;

            var style = (Style)m_Styles.SelectedItem;
            App.Settings.Style = style.Value;

            App.Settings.FontFamily = m_Fonts.SelectedIndex == 0 ? string.Empty : (string)m_Fonts.SelectedItem;

            var language = (Language)m_Languages.SelectedItem;
            App.Settings.Language = language.Code;

            App.Settings.EnableDragAndDrop = m_EnableDragAndDrop.IsChecked ?? false;
            App.Settings.ShowValidMoves = m_ShowValidMoves.IsChecked ?? true;

            App.Settings.ShowFileRankNotation = (Settings.FileRankNotations)m_ShowFileRankNotation.SelectedIndex;
            App.Settings.ShowSquareAttackIndicators = m_ShowSquareAttackIndicators.IsChecked ?? false;
            App.Settings.AttackColorMode = (Settings.SquareAttackColorMode)m_AttackColorMode.SelectedIndex;

            App.Settings.Topmost = m_Topmost.IsChecked ?? false;
            App.Settings.EnableAudio = m_EnableAudio.IsChecked ?? true;
            App.Settings.AutoSaveGameOnExit = m_SaveOnExit.IsChecked ?? true;
            App.Settings.AutoAnalyzeGames = m_AutoAnalyzeGames.IsChecked ?? false;
            App.Settings.AutoPauseWhenMinimized = m_AutoPauseWhenMinimized.IsChecked ?? true;

            App.Settings.MaxEngineThinkingTimeSecs = (int)m_MaxEngineThinkingTimeSecs.Value;

            if (m_MaxEngineDepth.Value > 0)
                App.Settings.MaxEngineDepth = (int)m_MaxEngineDepth.Value;
            else
                App.Settings.MaxEngineDepth = null;

            App.Settings.DefaultOpeningBook = m_OpeningBook.Value;

            App.Settings.PiecesSet = ((PiecesSet)m_PiecesSet.SelectedItem).Name;

            App.Settings.AccentColor = Utils.ColorConverter.ToHex(m_AccentButton.Color);
            App.Settings.HighlightColor = Utils.ColorConverter.ToHex(m_HighlightButton.Color);
            App.Settings.RestoreWindowSizeAndPosition = m_RestoreWindowSizeAndPosition.IsChecked ?? false;

            App.Settings.WhiteColor = Utils.ColorConverter.ToHex(m_WhiteButton.Color);
            App.Settings.WhiteSelectedColor = Utils.ColorConverter.ToHex(m_WhiteSelectedButton.Color);
            App.Settings.BlackColor = Utils.ColorConverter.ToHex(m_BlackButton.Color);
            App.Settings.BlackSelectedColor = Utils.ColorConverter.ToHex(m_BlackSelectedButton.Color);

            App.Settings.GameAnalysisEngineId = (m_AnalysisEngines.SelectedItem as EngineBase)?.Id;

            App.Settings.Save(App.SettingsPath);
            Result = true;

            await NavigateBack();
        } // OnOkClick

        private async void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Localizer.Localizer.Instance.LoadLanguage(App.Settings.Language);
            App.SetStyle(App.Settings.Style, App.Settings.AccentColor, App.Settings.HighlightColor, App.Settings.FontFamily);

            Result = false;
            await NavigateBack();
        } // OnCancelClick
   }
}