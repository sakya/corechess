using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChessLib.Engines;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace CoreChess.Views
{
    public class SettingsWindow : BaseView
    {
        private bool? m_Result;
        private AvaloniaColorPicker.ColorButton<ColorPickerWindow> m_AccentButton;
        private AvaloniaColorPicker.ColorButton<ColorPickerWindow> m_HighlightButton;
        private AvaloniaColorPicker.ColorButton<ColorPickerWindow> m_WhiteButton;
        private AvaloniaColorPicker.ColorButton<ColorPickerWindow> m_WhiteSelectedButton;
        private AvaloniaColorPicker.ColorButton<ColorPickerWindow> m_BlackButton;
        private AvaloniaColorPicker.ColorButton<ColorPickerWindow> m_BlackSelectedButton;

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

        public List<Style> m_Styles = new List<Style>()
        {
            new Style(Settings.Styles.Light, Localizer.Localizer.Instance["StyleLight"]),
            new Style(Settings.Styles.Dark, Localizer.Localizer.Instance["StyleDark"])
        };

        public List<Language> m_SupportedLanguages = new List<Language>()
        {
            new Language("en-US", "English"),
            new Language("it-IT", "Italian"),
        };

        public List<ColorTheme> m_ColorThemes = new List<ColorTheme>()
        {
            new ColorTheme("Custom", string.Empty, string.Empty, string.Empty, string.Empty),
            new ColorTheme("Default", "#ffeeeed2", "#fff7f783", "#ff769656", "#ffbbcb44"),
            new ColorTheme("Uscf", "#ffc3c6be", "#ffffffff", "#ff727fa2", "#ff465e9e"),
            new ColorTheme("Wikipedia", "#ffffce9e", "#ffffe3c8", "#ffd18b47", "#ffd17115"),
            new ColorTheme("Chess24", "#ff9e7863", "#ff9e8d83", "#ff633526", "#ff642612"),
            new ColorTheme("Leipzig", "#ffffffff", "#fff7f783", "#ffe1e1e1", "#ffbbcb44"),
            new ColorTheme("Symbol", "#ffffffff", "#ffc3c6be", "#ff58ac8a", "#ff18ab70"),
        };

        public SettingsWindow()
        {
            this.InitializeComponent();
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();

            this.Closing += OnWindowClosing;

            var cb = this.FindControl<ComboBox>("m_Fonts");
            var fonts = SkiaSharp.SKFontManager.Default.FontFamilies.OrderBy(f => f).ToList();
            fonts.Insert(0, "Default (Roboto)");
            cb.Items = fonts;
            cb.SelectedItem = fonts.Where(f => f == App.Settings.FontFamily).FirstOrDefault() ?? fonts[0];

            var grid = this.FindControl<Grid>("m_AppearenceGrid");
            m_AccentButton = new AvaloniaColorPicker.ColorButton<ColorPickerWindow>()
            {
                Margin = new Thickness(5, 5, 5, 5),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Color = Utils.ColorConverter.ParseHexColor(App.Settings.AccentColor)
            };
            m_AccentButton.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == AvaloniaColorPicker.ColorButton.ColorProperty.Name)
                    Application.Current.Resources["SystemAccentColor"] = m_AccentButton.Color;
            };
            Grid.SetColumn(m_AccentButton, 1);
            Grid.SetRow(m_AccentButton, 2);
            grid.Children.Add(m_AccentButton);

            m_HighlightButton = new AvaloniaColorPicker.ColorButton<ColorPickerWindow>()
            {
                Margin = new Thickness(5, 5, 5, 5),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Color = Utils.ColorConverter.ParseHexColor(App.Settings.HighlightColor)
            };
            m_HighlightButton.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == AvaloniaColorPicker.ColorButton.ColorProperty.Name)
                    Application.Current.Resources["HighlightColor"] = m_HighlightButton.Color;
            };
            Grid.SetColumn(m_HighlightButton, 1);
            Grid.SetRow(m_HighlightButton, 3);
            grid.Children.Add(m_HighlightButton);

            grid = this.FindControl<Grid>("m_ChessboardGrid");
            m_WhiteButton = new AvaloniaColorPicker.ColorButton<ColorPickerWindow>()
            {
                Margin = new Thickness(5, 5, 5, 5),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            };
            Grid.SetColumn(m_WhiteButton, 1);
            Grid.SetRow(m_WhiteButton, 5);
            grid.Children.Add(m_WhiteButton);

            m_WhiteSelectedButton = new AvaloniaColorPicker.ColorButton<ColorPickerWindow>()
            {
                Margin = new Thickness(5, 5, 5, 5),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            };
            Grid.SetColumn(m_WhiteSelectedButton, 1);
            Grid.SetRow(m_WhiteSelectedButton, 6);
            grid.Children.Add(m_WhiteSelectedButton);

            m_BlackButton = new AvaloniaColorPicker.ColorButton<ColorPickerWindow>()
            {
                Margin = new Thickness(5, 5, 5, 5),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            };
            Grid.SetColumn(m_BlackButton, 1);
            Grid.SetRow(m_BlackButton, 7);
            grid.Children.Add(m_BlackButton);

            m_BlackSelectedButton = new AvaloniaColorPicker.ColorButton<ColorPickerWindow>()
            {
                Margin = new Thickness(5, 5, 5, 5),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            };
            Grid.SetColumn(m_BlackSelectedButton, 1);
            Grid.SetRow(m_BlackSelectedButton, 8);
            grid.Children.Add(m_BlackSelectedButton);

            var txt = this.FindControl<TextBox>("m_PlayerName");
            txt.Text = App.Settings.PlayerName;

            cb = this.FindControl<ComboBox>("m_Styles");
            cb.Items = m_Styles;
            cb.SelectedItem = m_Styles.Where(l => l.Value == App.Settings.Style).FirstOrDefault();

            cb = this.FindControl<ComboBox>("m_Languages");
            cb.Items = m_SupportedLanguages;
            cb.SelectedItem = m_SupportedLanguages.Where(l => l.Code == Localizer.Localizer.Instance.Language).FirstOrDefault();

            var chk = this.FindControl<ToggleSwitch>("m_EnableDragAndDrop");
            chk.IsChecked = App.Settings.EnableDragAndDrop;
            chk = this.FindControl<ToggleSwitch>("m_ShowValidMoves");
            chk.IsChecked = App.Settings.ShowValidMoves;
            chk = this.FindControl<ToggleSwitch>("m_ShowFileRankNotation");
            chk.IsChecked = App.Settings.ShowFileRankNotation;

            chk = this.FindControl<ToggleSwitch>("m_Topmost");
            chk.IsChecked = App.Settings.Topmost;

            chk = this.FindControl<ToggleSwitch>("m_EnableAudio");
            chk.IsChecked = App.Settings.EnableAudio;
            chk = this.FindControl<ToggleSwitch>("m_SaveOnExit");
            chk.IsChecked = App.Settings.AutoSaveGameOnExit;
            chk = this.FindControl<ToggleSwitch>("m_AutoAnalyzeGames");
            chk.IsChecked = App.Settings.AutoAnalyzeGames;
            chk = this.FindControl<ToggleSwitch>("m_AutoPauseWhenMinimized");
            chk.IsChecked = App.Settings.AutoPauseWhenMinimized;

            cb = this.FindControl<ComboBox>("m_AnalysisEngines");
            cb.Items = App.Settings.Engines.OrderBy(e => e.Name);
            if (!string.IsNullOrEmpty(App.Settings.GameAnalysisEngineId))
                cb.SelectedItem = App.Settings.GetEngine(App.Settings.GameAnalysisEngineId);

            var nud = this.FindControl<NumericUpDown>("m_MaxEngineThinkingTimeSecs");
            nud.Value = App.Settings.MaxEngineThinkingTimeSecs;

            nud = this.FindControl<NumericUpDown>("m_MaxEngineDepth");
            nud.Value = App.Settings.MaxEngineDepth ?? 0;

            cb = this.FindControl<ComboBox>("m_OpeningBookType");
            if (string.IsNullOrEmpty(App.Settings.OpeningBook))
                cb.SelectedIndex = 0;
            else if (App.Settings.OpeningBook == Settings.InternalOpeningBook)
                cb.SelectedIndex = 1;
            else
                cb.SelectedIndex = 2;

            txt = this.FindControl<TextBox>("m_OpeningBook");
            txt.Text = App.Settings.OpeningBook;

            // Color theme
            cb = this.FindControl<ComboBox>("m_ColorTheme");
            cb.Items = m_ColorThemes;
            cb.SelectedItem = m_ColorThemes.Where(ct => ct.WhiteColor == App.Settings.WhiteColor &&
                ct.WhiteSelectedColor == App.Settings.WhiteSelectedColor &&
                ct.BlackColor == App.Settings.BlackColor &&
                ct.BlackSelectedColor == App.Settings.BlackSelectedColor)
                .FirstOrDefault();
            if (cb.SelectedItem == null) {
                // Custom color theme
                cb.SelectedIndex = 0;

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
                        if (sets.Where(s => s.Name == ps.Name).FirstOrDefault() == null) {
                            sets.Add(ps);

                            if (ps.Name == App.Settings.PiecesSet)
                                sel = ps;
                        }
                    }
                }
            }

            sets = sets.OrderBy(s => s.Name).ToList();
            var cmb = this.FindControl<ComboBox>("m_PiecesSet");
            cmb.Items = sets;
            cmb.SelectedItem = sel;
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
            SetWindowTitle();
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

        private void OnOpeningBookTypeChanged(object sender, SelectionChangedEventArgs args)
        {
            var cb = sender as ComboBox;
            var grid = this.FindControl<Grid>("m_OpeningBookCustom");
            var txt = this.FindControl<TextBox>("m_OpeningBook");
            if (cb.SelectedIndex == 0)
                txt.Text = string.Empty;
            else if (cb.SelectedIndex == 1)
                txt.Text = Settings.InternalOpeningBook;
            else if (cb.SelectedIndex == 2 && txt.Text == Settings.InternalOpeningBook)
                txt.Text = string.Empty;
            grid.IsVisible = cb.SelectedIndex == 2;
        } // OnOpeningBookTypeChanged

        private async void OnOpeningBookClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.AllowMultiple = false;
            dlg.Filters = new List<FileDialogFilter>()
            {
                new FileDialogFilter(){ Extensions = new List<string>() {"bin" }, Name = "Polyglot opening book"},
                new FileDialogFilter(){ Extensions = new List<string>() {"abk" }, Name = "Arena opening book"},
                new FileDialogFilter(){ Extensions = new List<string>() {"obk" }, Name = "Chessmaster opening book"},
            };
            string[] files = await dlg.ShowAsync(this);
            if (files?.Length > 0) {
                var txt = this.FindControl<TextBox>("m_OpeningBook");
                txt.Text = files[0];
            }
        } // OnOpeningBookClick

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            var txt = this.FindControl<TextBox>("m_PlayerName");
            App.Settings.PlayerName = txt.Text;

            var cb = this.FindControl<ComboBox>("m_Styles");
            var style = (Style)cb.SelectedItem;
            App.Settings.Style = style.Value;

            cb = this.FindControl<ComboBox>("m_Fonts");
            App.Settings.FontFamily = cb.SelectedIndex == 0 ? string.Empty : (string)cb.SelectedItem;

            cb = this.FindControl<ComboBox>("m_Languages");
            var language = (Language)cb.SelectedItem;
            App.Settings.Language = language.Code;

            var chk = this.FindControl<ToggleSwitch>("m_EnableDragAndDrop");
            App.Settings.EnableDragAndDrop = chk.IsChecked.Value;

            chk = this.FindControl<ToggleSwitch>("m_ShowValidMoves");
            App.Settings.ShowValidMoves = chk.IsChecked.Value;

            chk = this.FindControl<ToggleSwitch>("m_ShowFileRankNotation");
            App.Settings.ShowFileRankNotation = chk.IsChecked.Value;

            chk = this.FindControl<ToggleSwitch>("m_Topmost");
            App.Settings.Topmost = chk.IsChecked.Value;

            chk = this.FindControl<ToggleSwitch>("m_EnableAudio");
            App.Settings.EnableAudio = chk.IsChecked.Value;

            chk = this.FindControl<ToggleSwitch>("m_SaveOnExit");
            App.Settings.AutoSaveGameOnExit = chk.IsChecked.Value;

            chk = this.FindControl<ToggleSwitch>("m_AutoAnalyzeGames");
            App.Settings.AutoAnalyzeGames = chk.IsChecked.Value;

            chk = this.FindControl<ToggleSwitch>("m_AutoPauseWhenMinimized");
            App.Settings.AutoPauseWhenMinimized = chk.IsChecked.Value;

            var nud = this.FindControl<NumericUpDown>("m_MaxEngineThinkingTimeSecs");
            App.Settings.MaxEngineThinkingTimeSecs = (int)nud.Value;

            nud = this.FindControl<NumericUpDown>("m_MaxEngineDepth");
            if (nud.Value > 0)
                App.Settings.MaxEngineDepth = (int)nud.Value;
            else
                App.Settings.MaxEngineDepth = null;

            txt = this.FindControl<TextBox>("m_OpeningBook");
            App.Settings.OpeningBook = txt.Text;

            var cmb = this.FindControl<ComboBox>("m_PiecesSet");
            App.Settings.PiecesSet = ((PiecesSet)cmb.SelectedItem).Name;

            App.Settings.AccentColor = Utils.ColorConverter.ToHex(m_AccentButton.Color);
            App.Settings.HighlightColor = Utils.ColorConverter.ToHex(m_HighlightButton.Color);

            App.Settings.WhiteColor = Utils.ColorConverter.ToHex(m_WhiteButton.Color);
            App.Settings.WhiteSelectedColor = Utils.ColorConverter.ToHex(m_WhiteSelectedButton.Color);
            App.Settings.BlackColor = Utils.ColorConverter.ToHex(m_BlackButton.Color);
            App.Settings.BlackSelectedColor = Utils.ColorConverter.ToHex(m_BlackSelectedButton.Color);

            cmb = this.FindControl<ComboBox>("m_AnalysisEngines");
            App.Settings.GameAnalysisEngineId = (cmb.SelectedItem as EngineBase)?.Id;

            App.Settings.Save(App.SettingsPath);
            m_Result = true;
            this.Close(m_Result);
        } // OnOkClick

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Localizer.Localizer.Instance.LoadLanguage(App.Settings.Language);
            App.SetStyle(App.Settings.Style, App.Settings.AccentColor, App.Settings.HighlightColor, App.Settings.FontFamily);

            m_Result = false;
            this.Close(m_Result);
        } // OnCancelClick

        private void OnWindowClosing(object sender, CancelEventArgs args)
        {
            if (m_Result == null) {
                args.Cancel = true;
                OnCancelClick(null, null);
            }
        }
    }
}