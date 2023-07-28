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
                ExtendClientAreaToDecorationsHint = true;
                ExtendClientAreaTitleBarHeightHint = -1;
                ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
            }
            RestoreWindowSizeAndPosition();
        }

        public void SaveWindowSizeAndPosition()
        {
            // Save window size and position
            var ws = new WindowSize()
            {
                State = WindowState,

                Width = Bounds.Width,
                Height = Bounds.Height,

                Y = Position.Y,
                X = Position.X
            };
            ws.Save(Path.Join(App.LocalPath, $"ws{GetType().Name}.json"));
        } // SaveWindowSizeAndPosition

        public void RestoreWindowSizeAndPosition()
        {
            WindowSize ws;
            try {
                ws = WindowSize.Load(Path.Join(App.LocalPath, $"ws{GetType().Name}.json"));
                if (ws == null)
                    return;
            } catch {
                return;
            }

            var screen = Screens.ScreenFromPoint(Position);
            if (ws.State == WindowState.Maximized ||
                screen != null && ws.Width <= screen.Bounds.Width && ws.Height <= screen.Bounds.Height && ws.X <= screen.Bounds.Width && ws.Y <= screen.Bounds.Height) {
                WindowStartupLocation = WindowStartupLocation.Manual;
                WindowState = ws.State;
                if (ws.State == WindowState.Normal) {
                    Width = ws.Width;
                    Height = ws.Height;

                    Position = new PixelPoint(ws.X, ws.Y);
                }
            }
        } // RestoreWindowSizeAndPosition
    }
}