using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using System;
using Avalonia.Reactive;
using CoreChess.Views;

namespace CoreChess.Controls
{
    public partial class TitleBar : UserControl
    {
        bool m_CanGoBack;

        public TitleBar()
        {
            InitializeComponent();

            IsVisible = OperatingSystem.IsWindows();
            CanMinimize = true;
            CanMaximize = true;
        }

        public bool CanMinimize { get; set; }
        public bool CanMaximize { get; set; }

        public bool CanGoBack {
            get => m_CanGoBack;
            set
            {
                m_CanGoBack = value;
                BackBtn.IsVisible = m_CanGoBack;
                Icon.IsVisible = !m_CanGoBack;
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (this.VisualRoot is Window pw) {
                SetTitle(pw.Title);
                var title = pw.GetObservable(Window.TitleProperty);
                title.Subscribe(new AnonymousObserver<string>(SetTitle));

                var canResize = pw.GetObservable(Window.CanResizeProperty);
                canResize.Subscribe(new AnonymousObserver<bool>(value =>
                {
                    MaximizeBtn.IsEnabled = CanMaximize && value;
                }));

                var wState = pw.GetObservable(Window.WindowStateProperty);
                wState.Subscribe(new AnonymousObserver<WindowState>(s =>
                {
                    if (s == WindowState.Maximized) {
                        pw.Padding = new Thickness(5);
                        MaximizeBtn.Content = new Projektanker.Icons.Avalonia.Icon() { Value = "fas fa-window-restore" };
                    } else {
                        pw.Padding = new Thickness(0);
                        MaximizeBtn.Content = new Projektanker.Icons.Avalonia.Icon() { Value = "fas fa-window-maximize" };
                    }
                }));

                MinimizeBtn.Click += (s, a) =>
                {
                    pw.WindowState = WindowState.Minimized;
                };
                MinimizeBtn.IsVisible = CanMinimize;

                MaximizeBtn.Click += (s, a) =>
                {
                    if (VisualRoot is Window parentWindow) {
                        parentWindow.WindowState = parentWindow.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                    }
                };
                MaximizeBtn.IsVisible = CanMinimize;

                CloseBtn.Click += (s, a) =>
                {
                    pw.Close();
                };

                BackBtn.Click += async (s, a) =>
                {
                    await (pw as MainWindow)!.NavigateBack();
                };
            }
        }

        private void SetTitle(string title)
        {
            Title.Text = title;
        }
    }
}