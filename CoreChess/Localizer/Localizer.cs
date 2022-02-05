﻿namespace CoreChess.Localizer
{
    using Avalonia;
    using Avalonia.Platform;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Text;

    public class Localizer : INotifyPropertyChanged
    {
        private const string IndexerName = "Item";
        private const string IndexerArrayName = "Item[]";
        private Dictionary<string, string> m_Strings = null;

        public Localizer()
        {

        }

        public bool LoadLanguage(string language)
        {
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            Uri uri = new Uri($"avares://CoreChess/Assets/i18n/{language}.json");
            if (assets.Exists(uri)) {
                using (StreamReader sr = new StreamReader(assets.Open(uri), Encoding.UTF8)) {
                    m_Strings = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());
                }
                Language = language;
                Invalidate();

                return true;
            }
            return false;
        } // LoadLanguage

        public string Language { get; private set; }

        public string this[string key]
        {
            get
            {
                string res;
                if (m_Strings != null && m_Strings.TryGetValue(key, out res))
                    return res?.Replace("\\n", "\n");

                return $"{Language}:{key}";
            }
        }

        public static Localizer Instance { get; set; } = new Localizer();
        public event PropertyChangedEventHandler PropertyChanged;

        public void Invalidate()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerArrayName));
        }
    }
}