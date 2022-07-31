using Avalonia.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreChess
{
    public class WindowSize
    {
        public WindowSize()
        {
        }

        public WindowState State { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }

        public int Y { get; set; }
        public int X { get; set; }

        public void Save(string path)
        {
            using (StreamWriter sw = new StreamWriter(path)) {
                sw.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
            }
        } // Save

        public static WindowSize Load(string path)
        {
            using (StreamReader sr = new StreamReader(path)) {
                return JsonConvert.DeserializeObject<WindowSize>(sr.ReadToEnd());
            }
        } // Load
    }
}
