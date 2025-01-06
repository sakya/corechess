using Avalonia.Controls;
using System.Threading.Tasks;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public partial class WaitDialog : BaseDialog
    {
        public WaitDialog()
        {
            InitializeComponent();
            Animated = false;
            CloseOnBackdropClick = false;
        }

        public WaitDialog(string message)
        {
            InitializeComponent();

            Animated = false;
            CloseOnBackdropClick = false;
            m_Message.Text = message;
        }

        public static async Task<bool> ShowWaitWindow(Window owner, string message)
        {
            var dlg = new WaitDialog();
            await dlg.Show(owner);

            return true;
        } // ShowWaitWindow
    }
}
