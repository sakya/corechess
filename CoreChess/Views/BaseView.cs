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
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                this.ExtendClientAreaToDecorationsHint = true;
                this.ExtendClientAreaTitleBarHeightHint = -1;
                this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
            }

            SetWindowTitle();

            // Fix for https://github.com/AvaloniaUI/Avalonia/issues/6433
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var iv = this.GetObservable(Window.IsVisibleProperty);
                iv.Subscribe(value =>
                {
                    if (value && !m_CenterDone)
                    {
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

            Screen screen = null;
            while (screen == null)
            {
                await Task.Delay(1);
                screen = this.Screens.ScreenFromVisual(this);
            }

            if (this.WindowStartupLocation == WindowStartupLocation.CenterScreen)
            {
                var x = (int)Math.Floor(screen.Bounds.Width / 2 - this.Bounds.Width / 2);
                var y = (int)Math.Floor(screen.Bounds.Height / 2 - (this.Bounds.Height + 30) / 2);

                this.Position = new PixelPoint(x, y);
            }
            else if (this.WindowStartupLocation == WindowStartupLocation.CenterOwner)
            {
                var pw = this.Owner as Window;
                if (pw != null)
                {
                    var x = (int)Math.Floor(pw.Bounds.Width / 2 - this.Bounds.Width / 2 + pw.Position.X);
                    var y = (int)Math.Floor(pw.Bounds.Height / 2 - (this.Bounds.Height + 30) / 2 + pw.Position.Y);

                    this.Position = new PixelPoint(x, y);
                }
            }
        } // CenterWindow
    }
}
