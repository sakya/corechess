using Avalonia.Controls;
using Newtonsoft.Json;
using System.IO;

namespace CoreChess
{
    public class WindowSize
    {
        public WindowState State { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }

        public int Y { get; set; }
        public int X { get; set; }

        public void Save(string path)
        {
            using var sw = new StreamWriter(path);
            sw.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
        } // Save

        public static WindowSize Load(string path)
        {
            using var sr = new StreamReader(path);
            return JsonConvert.DeserializeObject<WindowSize>(sr.ReadToEnd());
        } // Load
    }
}
