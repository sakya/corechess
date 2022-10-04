using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Threading.Tasks;
using Avalonia.Input;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public class MessageDialog : BaseDialog
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
            var txt = this.FindControl<TextBlock>("m_Message");
            txt.Text = message;

            var btn1 = this.FindControl<Button>("m_Button1");
            var btn2 = this.FindControl<Button>("m_Button2");
            btn1.IsCancel = false;
            btn1.IsDefault = false;
            btn2.IsCancel = false;
            btn2.IsDefault = false;

            switch (buttons) {
                case Buttons.Ok:
                    btn2.IsVisible = false;
                    btn1.Content = Localizer.Localizer.Instance["Ok"];
                    btn1.IsCancel = true;
                    btn1.IsDefault = true;
                    break;
                case Buttons.OkCancel:
                    btn1.Content = Localizer.Localizer.Instance["Ok"];
                    btn1.IsDefault = true;
                    btn2.Content = Localizer.Localizer.Instance["Cancel"];
                    btn2.IsCancel = true;
                    break;
                case Buttons.YesNo:
                    btn1.Content = Localizer.Localizer.Instance["Yes"];
                    btn1.IsDefault = true;
                    btn2.Content = Localizer.Localizer.Instance["No"];
                    btn2.IsCancel = true;
                    break;
            }

            var i = this.FindControl<Projektanker.Icons.Avalonia.Icon>("m_Icon");
            switch (icon) {
                case Icons.None:
                    i.IsVisible = false;
                    break;
                case Icons.Error:
                    i.Value = "fas fa-exclamation-triangle";
                    i.Foreground = new SolidColorBrush((Color)App.MainWindow.FindResource("DangerColor"));
                    break;
                case Icons.Info:
                    i.Value = "fas fa-info-circle";
                    i.Foreground = new SolidColorBrush((Color)App.MainWindow.FindResource("InfoColor"));
                    break;
                case Icons.Question:
                    i.Value = "fas fa-question-circle";
                    i.Foreground = new SolidColorBrush((Color)App.MainWindow.FindResource("InfoColor"));
                    break;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            base.OnKeyDown(e);

            var btn1 = this.FindControl<Button>("m_Button1");
            var btn2 = this.FindControl<Button>("m_Button2");

            var cancelBtn = btn1.IsCancel  || btn2.IsCancel;
            var defaultBtn = btn1.IsDefault  || btn2.IsDefault;

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
            await _openedMessageDialog.Show<bool>(owner);

            return true;
        } // ShowMessage

        public static async Task<bool> ShowConfirmMessage(Window owner, string title, string message)
        {
            CloseOpenedWindow();
            _openedMessageDialog = new MessageDialog(title, message, Buttons.YesNo, Icons.Question);
            return await _openedMessageDialog.Show<bool>(owner);
        } // ShowConfirmMessage
        #endregion
    }
}
