using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using ChessLib;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public partial class PromotionDialog : BaseDialog
    {
        public PromotionDialog()
        {
            this.InitializeComponent();
        }

        public PromotionDialog(string imageFolder, ChessLib.Game.Colors color)
        {
            this.InitializeComponent();

            CloseOnBackdropClick = false;
            Color = color;

            // Draw pieces
            string col = "w";
            if (color == Game.Colors.Black)
                col = "b";

            var bitmap = new Bitmap($"{imageFolder}{System.IO.Path.DirectorySeparatorChar}{col}{Piece.Pieces.Knight.ToString()}.png");
            m_Knight.Content = new Image() { Source = bitmap, Height = 75 };

            bitmap = new Bitmap($"{imageFolder}{System.IO.Path.DirectorySeparatorChar}{col}{Piece.Pieces.Bishop.ToString()}.png");
            m_Bishop.Content = new Image() { Source = bitmap, Height = 75 };

            bitmap = new Bitmap($"{imageFolder}{System.IO.Path.DirectorySeparatorChar}{col}{Piece.Pieces.Rook.ToString()}.png");
            m_Rook.Content = new Image() { Source = bitmap, Height = 75 };

            bitmap = new Bitmap($"{imageFolder}{System.IO.Path.DirectorySeparatorChar}{col}{Piece.Pieces.Queen.ToString()}.png");
            m_Queen.Content = new Image() { Source = bitmap, Height = 75 };
        }

        public ChessLib.Game.Colors Color
        {
            get;
            private set;
        }

        public Piece.Pieces? Result
        {
            get;
            set;
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            Control btn = sender as Button;
            if (btn.Name == "m_Knight")
                Result = Piece.Pieces.Knight;
            else if (btn.Name == "m_Bishop")
                Result = Piece.Pieces.Bishop;
            else if (btn.Name == "m_Rook")
                Result = Piece.Pieces.Rook;
            else if (btn.Name == "m_Queen")
                Result = Piece.Pieces.Queen;

            this.Close(Result);
        }
    }
}
