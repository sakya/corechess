using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using ChessLib.Engines;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
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
                .AfterSetup(AfterSetupCallback)
                .UsePlatformDetect()
                .With(new Win32PlatformOptions { AllowEglInitialization = true })
                .LogToTrace()
                .UseSkia();
        }

        private static void AfterSetupCallback(AppBuilder appBuilder)
        {
            // Register icon provider(s)
            IconProvider.Register<FontAwesomeIconProvider>();
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
                mApp.MainWindow = await InitializeApp(args);
                mApp.MainWindow.Closed += (_, __) => stop.Cancel();
                mApp.MainWindow.Show();
                mApp.MainWindow.Activate();
                splash.Close();
            }
            Start();
            mApp.Run(stop.Token);
            Bass.BASS_Free();
        }

        static async Task<Views.MainWindow> InitializeApp(string[] args)
        {

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
                    await SetDefaultSettings();
                }
            } else {
                await SetDefaultSettings();
            }

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

            var res = new Views.MainWindow(args);
            // Load the ECO database
            await res.LoadEcoDatabase();

            return res;
        } // InitializeApp

        static async Task<bool> SetDefaultSettings()
        {
            // Default settings
            App.Settings = new Settings();

            App.Settings.EnableDragAndDrop = true;
            // Get current culture
            CultureInfo ci = CultureInfo.InstalledUICulture;
            App.Settings.Language = ci.Name;

            App.Settings.Engines = new List<EngineBase>();
            List<EngineBase> engines = new List<EngineBase>();

            // initialize engines (get options list)
            foreach (var engine in engines) {
                try {
                    await engine.Start();
                    await engine.Stop();
                    App.Settings.Engines.Add(engine);
                } catch {

                }
            }
            App.Settings.ActiveEngineId = App.Settings.Engines.Count > 0 ? App.Settings.Engines[0].Id : null;
            App.Settings.Save(App.SettingsPath);

            return true;
        } // SetDefaultSettings
    }
}
