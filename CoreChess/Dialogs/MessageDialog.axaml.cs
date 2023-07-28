using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Threading.Tasks;
using Avalonia.Input;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public partial class MessageDialog : BaseDialog
    {
        private static MessageDialog _openedMessageDialog = null;

        public enum Buttons
        {
            YesNo,
            OkCancel,
            Ok
        }

        public enum Icons
        {
            None,
            Error,
            Question,
            Info
        }

        public MessageDialog()
        {
        }

        public MessageDialog(string title, string message, Buttons buttons = Buttons.YesNo, Icons icon = Icons.None)
        {
            this.InitializeComponent();

            KeyDown += OnKeyDown;
            m_Message.Text = message;

            m_Button1.IsCancel = false;
            m_Button1.IsDefault = false;
            m_Button2.IsCancel = false;
            m_Button2.IsDefault = false;

            switch (buttons) {
                case Buttons.Ok:
                    m_Button2.IsVisible = false;
                    m_Button1.Content = Localizer.Localizer.Instance["Ok"];
                    m_Button1.IsCancel = true;
                    m_Button1.IsDefault = true;
                    break;
                case Buttons.OkCancel:
                    m_Button1.Content = Localizer.Localizer.Instance["Ok"];
                    m_Button1.IsDefault = true;
                    m_Button2.Content = Localizer.Localizer.Instance["Cancel"];
                    m_Button2.IsCancel = true;
                    break;
                case Buttons.YesNo:
                    m_Button1.Content = Localizer.Localizer.Instance["Yes"];
                    m_Button1.IsDefault = true;
                    m_Button2.Content = Localizer.Localizer.Instance["No"];
                    m_Button2.IsCancel = true;
                    break;
            }

            switch (icon) {
                case Icons.None:
                    m_Icon.IsVisible = false;
                    break;
                case Icons.Error:
                    m_Icon.Value = "fas fa-exclamation-triangle";
                    m_Icon.Foreground = new SolidColorBrush((Color)App.MainWindow.FindResource("DangerColor"));
                    break;
                case Icons.Info:
                    m_Icon.Value = "fas fa-info-circle";
                    m_Icon.Foreground = new SolidColorBrush((Color)App.MainWindow.FindResource("InfoColor"));
                    break;
                case Icons.Question:
                    m_Icon.Value = "fas fa-question-circle";
                    m_Icon.Foreground = new SolidColorBrush((Color)App.MainWindow.FindResource("InfoColor"));
                    break;
            }
        }

        public override void OnKeyDown(object sender, KeyEventArgs e)
        {
            base.OnKeyDown(e);

            var cancelBtn = m_Button1.IsCancel  || m_Button2.IsCancel;
            var defaultBtn = m_Button1.IsDefault  || m_Button2.IsDefault;

            if (e.Key == Key.Enter && defaultBtn) {
                e.Handled = true;
                Close(true);
            } else if (e.Key == Key.Escape && cancelBtn) {
                e.Handled = true;
                Close(false);
            }
        }

        private void OnButton1Click(object sender, RoutedEventArgs e)
        {
            this.Close(true);
        }

        private void OnButton2Click(object sender, RoutedEventArgs e)
        {
            this.Close(false);
        }

        #region static operations
        public static void CloseOpenedWindow()
        {
            if (_openedMessageDialog != null) {
                _openedMessageDialog.Close(false);
                _openedMessageDialog = null;
            }
        } // CloseOpenedWindow

        public static async Task<bool> ShowMessage(Window owner, string title, string message, Icons icon = Icons.None)
        {
            CloseOpenedWindow();
            _openedMessageDialog = new MessageDialog(title, message, Buttons.Ok, icon);
            await _openedMessageDialog.Show<bool?>(owner);

            return true;
        } // ShowMessage

        public static async Task<bool> ShowConfirmMessage(Window owner, string title, string message)
        {
            CloseOpenedWindow();
            _openedMessageDialog = new MessageDialog(title, message, Buttons.YesNo, Icons.Question);
            return await _openedMessageDialog.Show<bool?>(owner) == true;
        } // ShowConfirmMessage
        #endregion
    }
}
