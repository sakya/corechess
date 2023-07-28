using Avalonia.Interactivity;
using ChessLib;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public partial class MoveCommentDialog : BaseDialog
    {
        private readonly Game.MoveNotation m_Move;

        public MoveCommentDialog()
        {
            this.InitializeComponent();
        }

        public MoveCommentDialog(Game.MoveNotation move)
        {
            this.InitializeComponent();

            m_Move = move;
            m_Comment.Text = m_Move.Comment;
            m_Comment.AttachedToVisualTree += (s, e) => m_Comment.Focus();
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            m_Move.Comment = m_Comment.Text;
            this.Close(true);
        } // OnOkClick

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.Close(false);
        } // OnCancelClick
    }
}
