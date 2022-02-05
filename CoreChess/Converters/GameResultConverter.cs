using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CoreChess.Converters
{
    public class GameResultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string res = string.Empty;
            if (value is ChessLib.Game) {
                var game = (ChessLib.Game)value;
                if (game.Winner == ChessLib.Game.Colors.White)
                    res = "1-0";
                else if (game.Winner == ChessLib.Game.Colors.Black)
                    res = "0-1";
                else 
                    res = "1/2-1/2";

                if (game.Result == ChessLib.Game.Results.Timeout)
                    res = $"{res} (timeout)";
                else if (game.Result == ChessLib.Game.Results.Resignation)
                    res = $"{res} (resignation)";
            }
            return res;
        } // Convert

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
