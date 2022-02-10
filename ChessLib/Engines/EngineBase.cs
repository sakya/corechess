using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChessLib.Engines
{
    public abstract class EngineBase : IdObject
    {
        #region classes
        public class Option
        {
            public Option()
            {
                ValidValues = new List<string>();
            }

            public string Name { get; set; }
            public string Value { get; set; }
            public string Type { get; set; }
            public string Default { get; set; }
            public string Min { get; set; }
            public string Max { get; set; }
            public List<string> ValidValues { get; set; }
            /// <summary>
            /// Internal options won't be applyed by <see cref="ApplyOptions(bool)"/>
            /// </summary>
            public bool Internal { get; set; }
        } // Option

        public class BestMove
        {
            public BestMove(string move, string ponder)
            {
                Move = move;
                Ponder = ponder;
            }

            public string Move { get; set; }
            public string Ponder { get; set; }
        } // BestMove

        public class AnalyzeResult
        {
            public AnalyzeResult(Game.Colors colors, Info.ScoreValue score)
            {
                Color = colors;
                Score = score;
            }

            public Game.Colors Color { get; set; }
            public Info.ScoreValue Score { get; set; }
        } // AnalyzeResult

        public class Info
        {
            public class ScoreValue
            {
                public int CentiPawns { get; set; }
                public int MateIn { get; set; }
            }

            public ScoreValue Score { get; set; }
            public int? Depth { get; set; }
            public int? SelDepth { get; set; }
            public int? Time { get; set; }
            public int? Nodes { get; set; }
            public int? NodesPerSecond { get; set; }
            public int? MultiPv { get; set; }
            public string CurrMove { get; set; }
            public List<string> Pv { get; set; }
        } // Info
        #endregion

        #region events
        public delegate void ErrorHandler(object sender, string error);
        public event ErrorHandler Error;
        #endregion

        public EngineBase(string name, string command)
            : base()
        {
            Name = name;
            Command = command;
            Options = new List<Option>();
        }

        public string Name { get; set; }
        public string Command { get; set; }
        public string Arguments { get; set; }
        public string WorkingDir { get; set; }

        public string Author { get; set; }
        public Game.Colors PlayAs { get; set; }

        public List<Option> Options { get; set; }
        public abstract bool SupportChess960();
        public abstract bool SupportPondering();
        public abstract bool SupportAnalyze();
        public abstract Option GetChess960Option();
        public abstract bool IsPonderingEnabled();
        public abstract void SetPondering(bool enabled);
        public abstract bool IsOwnBookEnabled();
        public abstract void SetOwnBook(bool enabled);
        public abstract bool CanSetElo();
        public abstract bool SetElo(int elo);
        public abstract int? GetElo();
        public abstract int GetMinElo();
        public abstract int GetMaxElo();

        public abstract Task<bool> Start();
        public abstract Task<bool> Stop();
        public abstract Task<bool> SetOption(string name, string value);
        public abstract Task<bool> ApplyOptions(bool onlyModified);
        public abstract Task<bool> NewGame(int maxMinutes, int incrementSeconds);
        public abstract Task<bool> SetPosition(string fen, List<string> moves);
        public abstract Task<bool> ForceMove(string move);
        public abstract Task<BestMove> Ponder(int whiteTimeLeft, int whiteIncrement, int blackTimeLeft, int blackIncrement, int? depth, CancellationToken token, Action<string> outputCallback = null);
        public abstract Task<BestMove> GetBestMove(int whiteTimeLeft, int whiteIncrement, int blackTimeLeft, int blackIncrement, int? depth, TimeSpan? maxTimeSpan, CancellationToken token, List<string> searchMoves = null, Action<string> outputCallback = null);
        public abstract int GetDefaultAnalyzeDepth();
        public abstract Task<bool> EnterAnalyzeMode();
        public abstract Task<bool> ExitAnalyzeMode();
        public abstract Task<AnalyzeResult> Analyze(string fen, int? depth, TimeSpan? maxTimeSpan, CancellationToken token);
        public abstract Task<bool> StopCommand();
        public abstract Task<bool> Ponderhit();

        public abstract Info ParseThinkingInfo(string output);

        public Option GetOption(string name)
        {
            var res = Options?.Where(o => string.Compare(o.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0).FirstOrDefault();
            if (res == null && name.Contains("_")) {
                // Try to remove the underscores (Dragon 2.6 ha options "UCI Elo" and "UCI LimitStrength" intead of "UCI_Elo" and "UCI_LimitStrength")
                name = name.Replace("_", " ");
                res = Options?.Where(o => string.Compare(o.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0).FirstOrDefault();
            }
            return res;
        } // GetOption

        public string GetExePath(string fileName)
        {
            if (!File.Exists(fileName) && !Path.IsPathRooted(fileName)) {
                if (!string.IsNullOrEmpty(WorkingDir)) {
                    var temFileName = Path.Combine(WorkingDir, Command);
                    if (File.Exists(temFileName))
                        return temFileName;
                } else {
                    var folder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    var temFileName = Path.Combine(folder, fileName);
                    if (File.Exists(temFileName))
                        return temFileName;
                }
            }

            return fileName;
        } // GetExePath

        /// <summary>
        /// Returns a copy of the engine
        /// </summary>
        /// <returns></returns>
        public EngineBase Copy()
        {
            var sSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects };
            return JsonConvert.DeserializeObject<EngineBase>(JsonConvert.SerializeObject(this, sSettings), sSettings);
        } // Copy

        protected void RaiseError(string error)
        {
            Error?.Invoke(this, error);
        } // RaiseError
    }
}
