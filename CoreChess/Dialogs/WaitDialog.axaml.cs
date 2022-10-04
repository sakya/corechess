using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public class WaitDialog : BaseDialog
    {
        public WaitDialog()
        {
            this.InitializeComponent();
        }

        public WaitDialog(string message)
        {
            this.InitializeComponent();

            var txt = this.FindControl<TextBlock>("m_Message");
            txt.Text = message;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static async Task<bool> ShowWaitWindow(Window owner, string message)
        {
            var dlg = new WaitDialog();
            await dlg.Show(owner);

            return true;
        } // ShowWaitWindow
    }
}
