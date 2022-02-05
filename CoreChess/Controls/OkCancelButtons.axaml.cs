using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace CoreChess.Controls
{
    public class OkCancelButtons : UserControl
    {
        private string m_OkText;
        private bool m_OkEnabled;
        private string m_CancelText;
        private bool m_CancelVisible;
        public static readonly DirectProperty<OkCancelButtons, string> OkTextProperty =
                AvaloniaProperty.RegisterDirect<OkCancelButtons, string>(
                    nameof(OkText),
                    o => o.OkText,
                    (o, v) => o.OkText = v);


        public static readonly DirectProperty<OkCancelButtons, bool> OkEnabledProperty =
                AvaloniaProperty.RegisterDirect<OkCancelButtons, bool>(
                    nameof(OkEnabled),
                    o => o.OkEnabled,
                    (o, v) => o.OkEnabled = v);

        public static readonly DirectProperty<OkCancelButtons, string> CancelTextProperty =
                AvaloniaProperty.RegisterDirect<OkCancelButtons, string>(
                    nameof(CancelText),
                    o => o.CancelText,
                    (o, v) => o.CancelText = v);

        public static readonly DirectProperty<OkCancelButtons, bool> CancelVisibleProperty =
                AvaloniaProperty.RegisterDirect<OkCancelButtons, bool>(
                    nameof(CancelVisible),
                    o => o.CancelVisible,
                    (o, v) => o.CancelVisible = v);

        public event EventHandler<RoutedEventArgs> OkClick;
        public event EventHandler<RoutedEventArgs> CancelClick;

        public OkCancelButtons()
        {
            InitializeComponent();
        }

        public string OkText
        {
            get { return m_OkText; }
            set
            {
                SetAndRaise(OkTextProperty, ref m_OkText, value);
            }
        }

        public bool OkEnabled
        {
            get { return m_OkEnabled; }
            set
            {
                SetAndRaise(OkEnabledProperty, ref m_OkEnabled, value);
            }
        }

        public string CancelText
        {
            get { return m_CancelText; }
            set
            {
                SetAndRaise(CancelTextProperty, ref m_CancelText, value);
            }
        }

        public bool CancelVisible
        {
            get { return m_CancelVisible; }
            set
            {
                if (SetAndRaise(CancelVisibleProperty, ref m_CancelVisible, value)) {
                    var btn = this.FindControl<Button>("m_Ok");
                    btn.IsCancel = !value;
                    Grid.SetColumn(btn, value ? 1 : 0);
                    Grid.SetColumnSpan(btn, value ? 1 : 2);
                    if (value)
                        btn.Margin = new Thickness(5,0,0,0);
                    else
                        btn.Margin = new Thickness(0, 0, 0, 0);
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            OkEnabled = true;
            OkText = Localizer.Localizer.Instance["Ok"];
            CancelText = Localizer.Localizer.Instance["Cancel"];
            CancelVisible = true;
            DataContext = this;

            this.FindControl<Button>("m_Ok").Click += (s, args) => {
                OkClick?.Invoke(this, new RoutedEventArgs());
            };
            this.FindControl<Button>("m_Cancel").Click += (s, args) => {
                CancelClick?.Invoke(this, new RoutedEventArgs());
            };
        }
    }
}