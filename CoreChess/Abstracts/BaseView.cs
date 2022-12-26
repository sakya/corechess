using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System.Threading.Tasks;
using System;
using System.IO;

namespace CoreChess.Abstracts
{
    public abstract class BaseView : Window
    {
        protected virtual void InitializeComponent()
        {
#if DEBUG
            this.AttachDevTools();
#endif
            if (OperatingSystem.IsWindows()) {
                this.ExtendClientAreaToDecorationsHint = true;
                this.ExtendClientAreaTitleBarHeightHint = -1;
                this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
            }
        }

        protected override void OnOpened(EventArgs e)
        {
            // Fix for https://github.com/AvaloniaUI/Avalonia/issues/6433
            if (!OperatingSystem.IsWindows())
                CenterWindow();
        }

        private async void CenterWindow()
        {
            if (this.WindowStartupLocation == WindowStartupLocation.Manual)
                return;

            await Task.Delay(1);
            double scale = PlatformImpl?.DesktopScaling ?? 1.0;
            IWindowBaseImpl powner = Owner?.PlatformImpl;
            if (powner != null) {
                scale = powner.DesktopScaling;
            }
            PixelRect rect = new PixelRect(PixelPoint.Origin,
                PixelSize.FromSize(ClientSize, scale));
            if (WindowStartupLocation == WindowStartupLocation.CenterScreen) {
                Screen screen = Screens.ScreenFromPoint(powner?.Position ?? Position);
                if (screen == null)
                    return;
                Position = screen.WorkingArea.CenterRect(rect).Position;
            } else {
                if (powner == null || WindowStartupLocation != WindowStartupLocation.CenterOwner)
                    return;
                Position = new PixelRect(powner.Position,
                    PixelSize.FromSize(powner.ClientSize, scale)).CenterRect(rect).Position;
            }
        } // CenterWindow

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

            Screen screen = Screens.ScreenFromPoint(PlatformImpl.Position);
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