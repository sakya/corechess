using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using System.Reflection;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public class AboutDialog : BaseDialog
    {
        public AboutDialog()
        {
            this.InitializeComponent();

            var txt = this.FindControl<TextBlock>("m_Title");
            txt.Text = $"CoreChess{System.Environment.NewLine}v.{Assembly.GetEntryAssembly().GetName().Version}";

            txt = this.FindControl<TextBlock>("m_Copyright");
            txt.Text = ((AssemblyCopyrightAttribute)System.Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute), false)).Copyright;
        }

        protected void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter) {
                e.Handled = true;
                this.Close();
            }
        }
    }
}
