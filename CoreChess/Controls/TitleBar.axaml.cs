using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using System;
using CoreChess.Views;

namespace CoreChess.Controls
{
    public class TitleBar : UserControl
    {
        bool m_CanGoBack;

        public TitleBar()
        {
            InitializeComponent();

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
                var btn = this.FindControl<Button>("BackBtn");
                btn.IsVisible = m_CanGoBack;

                var icon = this.FindControl<Image>("Icon");
                icon.IsVisible = !m_CanGoBack;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            IsVisible = OperatingSystem.IsWindows();
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (this.VisualRoot is Window pw) {
                SetTitle(pw.Title);
                var title = pw.GetObservable(Window.TitleProperty);
                title.Subscribe(SetTitle);

                var canResize = pw.GetObservable(Window.CanResizeProperty);
                canResize.Subscribe(value =>
                {
                    this.FindControl<Button>("MaximizeBtn").IsEnabled = CanMaximize && value;
                });

                var wState = pw.GetObservable(Window.WindowStateProperty);
                wState.Subscribe(s =>
                {
                    var btn = this.FindControl<Button>("MaximizeBtn");
                    if (s == WindowState.Maximized) {
                        pw.Padding = new Thickness(5);
                        btn.Content = new Projektanker.Icons.Avalonia.Icon() { Value = "fas fa-window-restore" };
                    } else {
                        pw.Padding = new Thickness(0);
                        btn.Content = new Projektanker.Icons.Avalonia.Icon() { Value = "fas fa-window-maximize" };
                    }
                });

                var btn = this.FindControl<Button>("MinimizeBtn");
                btn.Click += (s, a) =>
                {
                    pw.WindowState = WindowState.Minimized;
                };
                btn.IsVisible = CanMinimize;

                btn = this.FindControl<Button>("MaximizeBtn");
                btn.Click += (s, a) =>
                {
                    if (VisualRoot is Window parentWindow) {
                        parentWindow.WindowState = parentWindow.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                    }
                };
                btn.IsVisible = CanMinimize;

                btn = this.FindControl<Button>("CloseBtn");
                btn.Click += (s, a) =>
                {
                    pw.Close();
                };

                btn = this.FindControl<Button>("BackBtn");
                btn.Click += async (s, a) =>
                {
                    await (pw as MainWindow)!.NavigateBack();
                };
            }
        }

        private void SetTitle(string title)
        {
            var txt = this.FindControl<TextBlock>("Title");
            txt.Text = title;
        }
    }
}