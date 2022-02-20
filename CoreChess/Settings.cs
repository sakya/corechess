﻿using ChessLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CoreChess
{
    public class Settings
    {
        public const string DefaultFontFamily = "avares://CoreChess/Assets/fonts#Roboto";
        public const string InternalOpeningBook = "./OpeningBooks/Perfect2021.bin";

        #region classes
        public class NewGameSettings
        {
            public string EngineId { get; set; }
            public int? EngineElo { get; set; }
            public Game.Colors? PlayerColor { get; set; }
            public bool Chess960 { get; set; }
            public TimeSpan? MaxTime { get; set; }
            public TimeSpan TimeIncrement { get; set; }
            public bool TrainingMode { get; set; }
            public string Personality { get; set; }
            public ChessLib.Engines.TheKing.Personality TheKingPersonality { get; set; }
        } // NewGameSettings

        #endregion

        public enum Notations
        {
            ShortAlgebraic,
            FigurineShortAlgebraic,
            LongAlgebraic,
            FigurineLongAlgebraic,
            Coordinate
        }

        public enum CapturedPiecesDisplay
        {
            All,
            Difference
        }

        public enum Styles
        {
            Dark,
            Light
        }

        public Settings()
        {
            ShowEngineOutput = true;
            MoveNotation = Notations.ShortAlgebraic;
            PlayerName = "Player 1";
            Language = "en-US";
            EnableAudio = true;
            ShowValidMoves = true;
            ShowFileRankNotation = true;
            AutoSaveGameOnExit = true;
            AutoPauseWhenMinimized = true;

            AccentColor = "#cf6641";
            HighlightColor = "#fd7d00";

            PiecesSet = "Default";
            WhiteColor = "#ffeeeed2";
            WhiteSelectedColor = "#fff7f783";
            BlackColor = "#ff769656";
            BlackSelectedColor = "#ffbbcb44";

            OpeningBook = InternalOpeningBook;
        }

        public string Version { get; set; }
        public string Language { get; set; }

        [JsonIgnore]
        public CultureInfo Culture
        {
            get
            {
                if (!string.IsNullOrEmpty(Language))
                    return CultureInfo.GetCultureInfo(Language);
                return CultureInfo.InvariantCulture;
            }
        }

        public bool Topmost { get; set; }

        public string PlayerName { get; set; }
        public bool EnableAudio { get; set; }
        public bool EnableDragAndDrop { get; set; }
        public bool ShowValidMoves { get; set; }
        public bool ShowFileRankNotation { get; set; }
        public bool AutoSaveGameOnExit { get; set; }
        public bool AutoPauseWhenMinimized { get; set; }
        public int MaxEngineThinkingTimeSecs { get; set; }
        public int? MaxEngineDepth { get; set; }

        public string AccentColor { get; set; }
        public string HighlightColor { get; set; }
        public string FontFamily { get; set; }

        public string PiecesSet { get; set; }
        public string WhiteColor { get; set; }
        public string WhiteSelectedColor { get; set; }
        public string BlackColor { get; set; }
        public string BlackSelectedColor { get; set; }
        public string OpeningBook { get; set; }
        public bool ShowEngineOutput { get; set; }
        public bool AutoAnalyzeGames { get; set; }
        public Notations MoveNotation { get; set; }
        public CapturedPiecesDisplay CapturedPieces { get; set; }

        public NewGameSettings NewGame { get; set; }

        public Styles Style { get; set; }
        /// <summary>
        /// Available engines
        /// </summary>
        public List<ChessLib.Engines.EngineBase> Engines { get; set; }

        /// <summary>
        /// Id of the engine used for game analysis
        /// </summary>
        public string GameAnalysisEngineId { get; set; }
        /// <summary>
        /// Engine for analysis with options
        /// </summary>
        [JsonIgnore]
        public ChessLib.Engines.EngineBase GameAnalysisEngine
        {
            get
            {
                if (!string.IsNullOrEmpty(GameAnalysisEngineId))
                    return GetEngine(GameAnalysisEngineId);
                return null;
            }
        }

        /// <summary>
        /// Get the engine with the given Id
        /// </summary>
        /// <param name="id">Th engine id</param>
        /// <returns></returns>
        public ChessLib.Engines.EngineBase GetEngine(string id)
        {
            return Engines?.Where(e => e.Id == id).FirstOrDefault();
        } // GetEngine

        public static Settings Load(string path)
        {
            using (StreamReader sr = new StreamReader(path)) {
                var settings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects };
                return JsonConvert.DeserializeObject<Settings>(sr.ReadToEnd(), settings);
            }
        } // Load

        public void Save(string path)
        {
            using (StreamWriter sw = new StreamWriter(path)) {
                Version = App.Version;

                var settings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects };
                sw.Write(JsonConvert.SerializeObject(this, Formatting.Indented, settings));
            }
        } // Save
    } // Settings
}
