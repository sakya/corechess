using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;

namespace CoreChess.Views
{
    public class WaitWindow : BaseView
    {
        public WaitWindow()
        {
            this.InitializeComponent();
        }

        public WaitWindow(string message)
        {
            this.InitializeComponent();

            var txt = this.FindControl<TextBlock>("m_Message");
            txt.Text = message;
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();
        }

        public static async Task<bool> ShowWaitWindow(Window owner, string message)
        {
            var dlg = new WaitWindow();
            await dlg.ShowDialog(owner);

            return true;
        } // ShowWaitWindow
    }
}
