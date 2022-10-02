using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using CoreChess.Views;

namespace CoreChess
{
    public class App : Application
    {
        public static string Version { get; set; }
        public static string BinaryPath { get; set; }
        public static string LocalPath { get; set; }
        public static string SettingsPath { get; set; }
        public static string GamesDatabasePath { get; set; }
        public static string PiecesPath { get; set; }
        public static string LocalPiecesPath { get; set; }
        public static Settings.Styles? CurrentStyle { get; private set; } = Settings.Styles.Dark;
        public static MainWindow MainWindow { get; set; }
        public static Settings Settings
        {
            get; set;
        }

        public static void SetStyle(Settings.Styles style, string accentColor, string highlightColor, string fontFamily)
        {
            Avalonia.Styling.Styles styles = null;

            if (style != CurrentStyle) {
                if (style == Settings.Styles.Dark) {
                    styles = new Avalonia.Styling.Styles() {
                        new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("resm:Styles?assembly=CoreChess"))
                        {
                            Source = new Uri("avares://Avalonia.Themes.Default/Accents/BaseDark.xaml")
                        },
                        new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("resm:Styles?assembly=CoreChess"))
                        {
                            Source = new Uri("avares://Avalonia.Themes.Fluent/FluentDark.xaml")
                        },
                        new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("resm:Styles?assembly=CoreChess"))
                        {
                            Source = new Uri("avares://CoreChess/Assets/Styles/PaletteDark.axaml")
                        },
                    };
                } else {
                    styles = new Avalonia.Styling.Styles() {
                        new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("resm:Styles?assembly=CoreChess"))
                        {
                            Source = new Uri("avares://Avalonia.Themes.Default/Accents/BaseLight.xaml")
                        },
                        new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("resm:Styles?assembly=CoreChess"))
                        {
                            Source = new Uri("avares://Avalonia.Themes.Fluent/FluentLight.xaml")
                        },
                        new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("resm:Styles?assembly=CoreChess"))
                        {
                            Source = new Uri("avares://CoreChess/Assets/Styles/PaletteLight.axaml")
                        },
                    };
                }

                for (int i=0; i < styles.Count; i++) {
                    Application.Current.Styles[i] = styles[i];
                }
            }

            if (string.IsNullOrEmpty(fontFamily))
                fontFamily = Settings.DefaultFontFamily;
            Application.Current.Resources["DefaultFontFamily"] = new Avalonia.Media.FontFamily(fontFamily);

            if (!string.IsNullOrEmpty(accentColor))
                Application.Current.Resources["SystemAccentColor"] = Utils.ColorConverter.ParseHexColor(accentColor);
            if (!string.IsNullOrEmpty(highlightColor))
                Application.Current.Resources["HighlightColor"] = Utils.ColorConverter.ParseHexColor(highlightColor);

            CurrentStyle = style;
        } // SetStyle

        /// <summary>
        /// Get the piece set path.
        /// First look into local folder, then in application folder
        /// </summary>
        /// <param name="setName">The chess set name</param>
        /// <returns>The chess set path</returns>
        public static string GetPiecesPath(string setName)
        {
            var files = new List<string>()
            {
                "bBishop.png", "bKing.png", "bKnight.png", "bPawn.png", "bQueen.png", "bRook.png",
                "wBishop.png", "wKing.png", "wKnight.png", "wPawn.png", "wQueen.png", "wRook.png"
            };

            var paths = new List<string>()
            {
                System.IO.Path.Combine(App.LocalPiecesPath, setName),
                System.IO.Path.Combine(App.PiecesPath, setName)
            };

            foreach (var p in paths) {
                if (Directory.Exists(p)) {
                    bool valid = true;
                    foreach (var f in files) {
                        if (!File.Exists(Path.Combine(p, f))) {
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                        return p;
                }
            }
            return Path.Combine(App.PiecesPath, "Default");
        } // GetPiecesPath

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void Run(CancellationToken ct)
        {
            try {
                Dispatcher.UIThread.MainLoop(ct);
            } catch (Exception ex) {
                // Log the exception
                using (var sw = new StreamWriter(System.IO.Path.Combine(LocalPath, "crash.log"))) {
                    Exception lex = ex;
                    while (lex != null) {
                        sw.WriteLine($"[{DateTime.Now.ToString("yyyyMMdd_HHmmss")}] {lex.Message}");
                        if (!string.IsNullOrEmpty(lex.StackTrace))
                            sw.WriteLine($"[{DateTime.Now.ToString("yyyyMMdd_HHmmss")}] {lex.StackTrace}");

                        lex = lex.InnerException;
                    }
                    sw.Flush();
                }
            }
        } // Run

        public static Color? GetStyleColor(string name)
        {
            if (Current is App app && App.MainWindow != null) {
                var resource = App.MainWindow.FindResource(name);
                if (resource is Color col)
                    return col;
            }
            return null;
        }
    }
}
