using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreChess.Utils
{
    static class ColorConverter
    {
        private const int s_zeroChar = (int)'0';
        private const int s_aLower = (int)'a';
        private const int s_aUpper = (int)'A';

        public static int ParseHexChar(char c)
        {
            int intChar = (int)c;

            if ((intChar >= s_zeroChar) && (intChar <= (s_zeroChar + 9))) {
                return (intChar - s_zeroChar);
            }

            if ((intChar >= s_aLower) && (intChar <= (s_aLower + 5))) {
                return (intChar - s_aLower + 10);
            }

            if ((intChar >= s_aUpper) && (intChar <= (s_aUpper + 5))) {
                return (intChar - s_aUpper + 10);
            }

            throw new Exception("Error converting hex char");
        } // ParseHexChar

        public static Color ParseHexColor(string color)
        {
            if (string.IsNullOrEmpty(color))
                return Colors.Transparent;

            int a, r, g, b;
            a = 255;

            if (color.Length > 7) {
                a = (ParseHexChar(color[1]) * 16) + ParseHexChar(color[2]);
                r = (ParseHexChar(color[3]) * 16) + ParseHexChar(color[4]);
                g = (ParseHexChar(color[5]) * 16) + ParseHexChar(color[6]);
                b = (ParseHexChar(color[7]) * 16) + ParseHexChar(color[8]);
            } else if (color.Length > 5) {
                r = (ParseHexChar(color[1]) * 16) + ParseHexChar(color[2]);
                g = (ParseHexChar(color[3]) * 16) + ParseHexChar(color[4]);
                b = (ParseHexChar(color[5]) * 16) + ParseHexChar(color[6]);
            } else if (color.Length > 4) {
                a = ParseHexChar(color[1]);
                a = a + (a * 16);
                r = ParseHexChar(color[2]);
                r = r + (r * 16);
                g = ParseHexChar(color[3]);
                g = g + (g * 16);
                b = ParseHexChar(color[4]);
                b = b + (b * 16);
            } else {
                r = ParseHexChar(color[1]);
                r = r + (r * 16);
                g = ParseHexChar(color[2]);
                g = g + (g * 16);
                b = ParseHexChar(color[3]);
                b = b + (b * 16);
            }

            return (Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b));
        } // ParseHexColor

        public static string ToHex(Color color)
        {
            return $"#{color.A.ToString("X2").ToLower()}{color.R.ToString("X2").ToLower()}{color.G.ToString("X2").ToLower()}{color.B.ToString("X2").ToLower()}";
        }
    }
}
