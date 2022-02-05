using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChessLib;

namespace CoreChess.Views
{
    public class MoveCommentWindow : BaseView
    {
        private Game.MoveNotation m_Move = null;

        public MoveCommentWindow()
        {
            this.InitializeComponent();
        }

        public MoveCommentWindow(Game.MoveNotation move)
        {
            this.InitializeComponent();

            m_Move = move;
            var txt = this.FindControl<TextBox>("m_Comment");
            txt.Text = m_Move.Comment;
            txt.AttachedToVisualTree += (s, e) => txt.Focus();
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            var txt = this.FindControl<TextBox>("m_Comment");
            m_Move.Comment = txt.Text;
            this.Close(true);
        } // OnOkClick

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.Close(false);
        } // OnCancelClick
    }
}
