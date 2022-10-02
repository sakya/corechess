using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using ChessLib;

namespace CoreChess.Views
{
    public class PromotionWindow : BaseView
    {
        public PromotionWindow()
        {
        }

        public PromotionWindow(string imageFolder, ChessLib.Game.Colors color)
        {
            this.InitializeComponent();

            Color = color;

            // Draw pieces
            string col = "w";
            if (color == Game.Colors.Black)
                col = "b";

            var btn = this.FindControl<Button>("m_Knight");
            var bitmap = new Bitmap($"{imageFolder}{System.IO.Path.DirectorySeparatorChar}{col}{Piece.Pieces.Knight.ToString()}.png");
            btn.Content = new Image() { Source = bitmap, Height = 75 };

            btn = this.FindControl<Button>("m_Bishop");
            bitmap = new Bitmap($"{imageFolder}{System.IO.Path.DirectorySeparatorChar}{col}{Piece.Pieces.Bishop.ToString()}.png");
            btn.Content = new Image() { Source = bitmap, Height = 75 };

            btn = this.FindControl<Button>("m_Rook");
            bitmap = new Bitmap($"{imageFolder}{System.IO.Path.DirectorySeparatorChar}{col}{Piece.Pieces.Rook.ToString()}.png");
            btn.Content = new Image() { Source = bitmap, Height = 75 };

            btn = this.FindControl<Button>("m_Queen");
            bitmap = new Bitmap($"{imageFolder}{System.IO.Path.DirectorySeparatorChar}{col}{Piece.Pieces.Queen.ToString()}.png");
            btn.Content = new Image() { Source = bitmap, Height = 75 };

            this.Closing += (s, e) =>
            {
                if (Result == null)
                    e.Cancel = true;
            };
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

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            IControl btn = sender as Button;
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
