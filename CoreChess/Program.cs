using Avalonia;
using ChessLib.Engines;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform;
using Un4seen.Bass;

namespace CoreChess
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            // Set current directory to the one containing the executable
            string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            Directory.SetCurrentDirectory(exeDir);

            BuildAvaloniaApp()
            .Start(AppMain, args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new X11PlatformOptions { UseGpu = false })
                .With(new Win32PlatformOptions { AllowEglInitialization = false })
                .LogToTrace()
                .UseSkia()
                .WithIcons(container => container.Register<FontAwesomeIconProvider>());
        }

        private static void AppMain(Application app, string[] args)
        {
            var stop = new CancellationTokenSource();
            var mApp = ((App)app);

            // Init audio
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

            async void Start()
            {
                var splash = new Views.SplashWindow();
                splash.Show();
                await Task.Delay(100);
                App.MainWindow = await InitializeApp(args);
                App.MainWindow.Closed += (_, __) => stop.Cancel();
                App.MainWindow.Show();
                App.MainWindow.Activate();
                splash.Close();
            }
            Start();
            mApp.Run(stop.Token);
            Bass.BASS_Free();
        }

        private static async Task<Views.MainWindow> InitializeApp(string[] args)
        {
            App.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            App.BinaryPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            App.LocalPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoreChess");
            if (!Directory.Exists(App.LocalPath))
                Directory.CreateDirectory(App.LocalPath);
            App.PiecesPath = $"Images{Path.DirectorySeparatorChar}Pieces";
            App.LocalPiecesPath = Path.Combine(App.LocalPath, "Images", "Pieces");

            // Load settings
            App.SettingsPath = Path.Join(App.LocalPath, "settings.json");
            if (File.Exists(App.SettingsPath)) {
                try {
                    App.Settings = Settings.Load(App.SettingsPath);
                } catch {
                    SetDefaultSettings();
                }
            } else {
                SetDefaultSettings();
            }
            await CheckEngines();

            App.SetStyle(App.Settings.Style, App.Settings.AccentColor, App.Settings.HighlightColor, App.Settings.FontFamily);

            App.GamesDatabasePath = Path.Join(App.LocalPath, "GamesDatabase");
            if (!Directory.Exists(App.GamesDatabasePath))
                Directory.CreateDirectory(App.GamesDatabasePath);

            try {
                if (!Localizer.Localizer.Instance.LoadLanguage(App.Settings.Language))
                    throw new Exception($"Failed to load language {App.Settings.Language}");
            } catch (Exception) {
                App.Settings.Language = "en-US";
                Localizer.Localizer.Instance.LoadLanguage(App.Settings.Language);
                App.Settings.Save(App.SettingsPath);
            }

            // Load ECO database
            App.EcoDatabase = new Utils.EcoDatabase();
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            Uri uri = new Uri($"avares://CoreChess/Assets/eco.pgn");
            using (Stream s = assets.Open(uri)) {
                await App.EcoDatabase.Load(s);
            }

            var res = new Views.MainWindow(args);
            return res;
        } // InitializeApp

        private static bool SetDefaultSettings()
        {
            // Default settings
            App.Settings = new Settings();

            App.Settings.EnableDragAndDrop = true;
            // Get current culture
            CultureInfo ci = CultureInfo.InstalledUICulture;
            App.Settings.Language = ci.Name;

            App.Settings.Engines = new List<EngineBase>();
            App.Settings.Save(App.SettingsPath);

            return true;
        } // SetDefaultSettings

        private static async Task<bool> CheckEngines()
        {
            List<EngineBase> engines = App.Settings.Engines ?? new List<EngineBase>();

            // Remove invalid engines
            for (int i = 0; i < engines.Count; i++) {
                if (!ExistsInPath(engines[i].Command)) {
                    engines.RemoveAt(i);
                    i--;
                }
            }

            // Fix for gnuchess in flatpak
            if (App.Settings.Version == "0.10.5.0" && App.Version == "0.10.5.1") {
                var gnuchess = engines.Where(e => e.Command == "/app/bin/Engines/gnuchess/gnuchess").FirstOrDefault();
                if (gnuchess != null)
                    engines.Remove(gnuchess);
            }

            List<EngineBase> defaultEngines = new List<EngineBase>();
            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                // Check nnue
                var sf = engines.Where(e => e.Command == "/app/bin/Engines/stockfish/stockfish").FirstOrDefault();
                if (sf != null) {
                    var efo = sf.GetOption("EvalFile");
                    if (efo != null) {
                        if (string.IsNullOrEmpty(efo.Value) || !File.Exists($"/app/bin/Engines/stockfish/{efo.Value}")) {
                            var evalFile = Directory.GetFiles("/app/bin/Engines/stockfish/", "*.nnue").FirstOrDefault();
                            if (!string.IsNullOrEmpty(evalFile))
                                efo.Value = Path.GetFileName(evalFile);
                            else
                                efo.Value = string.Empty;
                        }
                    }
                }

                // Add default engines (flatpak)
                defaultEngines.Add(
                    new Uci("Stockfish", "/app/bin/Engines/stockfish/stockfish")
                    {
                        WorkingDir = "/app/bin/Engines/stockfish"
                    }
                );
                defaultEngines.Add(
                    new Uci("Leela Chess Zero", "/app/bin/Engines/lc0/lc0")
                    {
                        WorkingDir = "/app/bin/Engines/lc0"
                    }
                );
                defaultEngines.Add(
                    new Uci("Gnuchess", "/app/bin/Engines/gnuchess/gnuchess")
                    {
                        WorkingDir = "/app/bin/Engines/gnuchess",
                        Arguments = "--uci"
                    }
                );
            } else if (OperatingSystem.IsWindows()) {
                // Add default engines (Inno Setup)
                defaultEngines.Add(
                    new Uci("Stockfish", Path.Combine(App.BinaryPath, @"Engines\stockfish\stockfish-windows-2022-x86-64.exe"))
                    {
                        WorkingDir = Path.Combine(App.BinaryPath, @"Engines\stockfish")
                    }
                );
                defaultEngines.Add(
                    new Uci("Leela Chess Zero", Path.Combine(App.BinaryPath, @"Engines\lc0\lc0.exe"))
                    {
                        WorkingDir = Path.Combine(App.BinaryPath, @"Engines\lc0")
                    }
                );
            }

            // Check missing default engines
            foreach (var dEngine in defaultEngines) {
                var existingEngine = engines.Where(e => e.Command == dEngine.Command).FirstOrDefault();
                if (existingEngine == null) {
                    // Initialize engine (get options list)
                    try {
                        await dEngine.Start();
                        await dEngine.Stop();

                        dEngine.SetPondering(true);
                        dEngine.SetOwnBook(false);

                        engines.Add(dEngine);
                    } catch {
                    }
                }
            }

            App.Settings.Engines = engines;
            App.Settings.Save(App.SettingsPath);

            return true;
        } // CheckEngines

        private static bool ExistsInPath(string fileName)
        {
            if (File.Exists(fileName))
                return true;

            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in pathEnv.Split(Path.PathSeparator)) {
                if (File.Exists(Path.Combine(path, fileName)))
                    return true;
            }
            return false;
        } // ExistsInPath
    }
}
