using Avalonia.Input;
using System.Reflection;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public partial class AboutDialog : BaseDialog
    {
        public AboutDialog()
        {
            this.InitializeComponent();

            m_Title.Text = $"CoreChess{System.Environment.NewLine}v.{Assembly.GetEntryAssembly()?.GetName().Version}";
            m_Copyright.Text = ((AssemblyCopyrightAttribute)System.Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute), false))?.Copyright;
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
