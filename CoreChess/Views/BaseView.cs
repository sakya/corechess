using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreChess.Views
{
    public abstract class BaseView : Window
    {
        private bool m_CenterDone = false;

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

            SetWindowTitle();

            // Fix for https://github.com/AvaloniaUI/Avalonia/issues/6433
            if (!OperatingSystem.IsWindows()) {
                var iv = this.GetObservable(Window.IsVisibleProperty);
                iv.Subscribe(value => {
                    if (value && !m_CenterDone) {
                        m_CenterDone = true;
                        CenterWindow();
                    }
                });
            }
        } // InitializeComponent

        protected void SetWindowTitle()
        {
            string title = Localizer.Localizer.Instance[$"WT_{this.GetType().Name}"];
            if (!string.IsNullOrEmpty(title))
                this.Title = $"CoreChess - {title}";
        }

        private async void CenterWindow()
        {
            if (this.WindowStartupLocation == WindowStartupLocation.Manual)
                return;

            await Task.Delay(1);
            double scale = PlatformImpl?.DesktopScaling ?? 1.0;
            IWindowBaseImpl powner = Owner?.PlatformImpl;
            if(powner != null) {
                scale = powner.DesktopScaling;
            }
            PixelRect rect = new PixelRect(PixelPoint.Origin,
                PixelSize.FromSize(ClientSize, scale));
            if(WindowStartupLocation == WindowStartupLocation.CenterScreen) {
                Screen screen = Screens.ScreenFromPoint(powner?.Position ?? Position);
                if(screen == null) {
                    return;
                }
                Position = screen.WorkingArea.CenterRect(rect).Position;
            }
            else {
                if(powner == null ||
                    WindowStartupLocation != WindowStartupLocation.CenterOwner) {
                    return;
                }
                Position = new PixelRect(powner.Position,
                    PixelSize.FromSize(powner.ClientSize, scale)).CenterRect(rect).Position;
            }
        } // CenterWindow
    }
}
