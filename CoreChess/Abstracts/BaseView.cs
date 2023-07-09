using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.IO;

namespace CoreChess.Abstracts
{
    public abstract class BaseView : Window
    {
        protected virtual void InitializeComponent()
        {
            if (OperatingSystem.IsWindows()) {
                this.ExtendClientAreaToDecorationsHint = true;
                this.ExtendClientAreaTitleBarHeightHint = -1;
                this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
            }
        }

        public void SaveWindowSizeAndPosition()
        {
            // Save window size and position
            var ws = new WindowSize()
            {
                State = this.WindowState,

                Width = this.Bounds.Width,
                Height = this.Bounds.Height,

                Y = this.Position.Y,
                X = this.Position.X
            };
            ws.Save(Path.Join(App.LocalPath, $"ws{this.GetType().Name}.json"));
        } // SaveWindowSizeAndPosition

        public void RestoreWindowSizeAndPosition()
        {
            WindowSize ws;
            try {
                ws = WindowSize.Load(Path.Join(App.LocalPath, $"ws{this.GetType().Name}.json"));
                if (ws == null)
                    return;
            } catch {
                return;
            }

            Screen screen = Screens.ScreenFromPoint(this.Position);
            if (ws.State == WindowState.Maximized ||
                screen != null && ws.Width <= screen.Bounds.Width && ws.Height <= screen.Bounds.Height && ws.X <= screen.Bounds.Width && ws.Y <= screen.Bounds.Height) {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.WindowState = ws.State;
                if (ws.State == WindowState.Normal) {
                    this.Width = ws.Width;
                    this.Height = ws.Height;

                    this.Position = new PixelPoint(ws.X, ws.Y);
                }
            }
        } // RestoreWindowSizeAndPosition
    }
}