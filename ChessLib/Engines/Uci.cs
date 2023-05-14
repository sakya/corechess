using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChessLib.Engines
{
    /// <summary>
    /// UCI chess engine
    /// http://wbec-ridderkerk.nl/html/UCIProtocol.html
    /// </summary>
    public class Uci : EngineBase
    {
        public const string Chess960OptionName = "UCI_Chess960";
        public const string AnalyzeModeOptionName = "UCI_AnalyseMode";
        public const string PonderOptionName = "Ponder";
        public const string OwnBookOptionName = "OwnBook";
        public const string EloOptionName = "UCI_Elo";
        public const string LimitStrengthOptionName = "UCI_LimitStrength";
        public static readonly string[] PersonalityOptionNames = new string[] { "Personality" };

        private StringBuilder m_EngineError = new StringBuilder();
        private StringBuilder m_ProcessOutput = new StringBuilder();
        private Semaphore m_ProcessOutputSema = null;
        private Semaphore m_ProcessInputSema = null;
        private Process m_Process = null;
        public Uci(string name, string command)
            : base(name, command)
        {
        }

        public string RegisterName { get; set; }
        public string RegisterCode { get; set; }

        public override async Task<bool> Start()
        {
            await Stop();

            m_ProcessOutputSema = new Semaphore(1, 1);
            m_ProcessInputSema = new Semaphore(1, 1);

            ProcessStartInfo si = new ProcessStartInfo();
            si.FileName = GetExePath(Command);
            si.Arguments = Arguments;
            si.WorkingDirectory = string.IsNullOrEmpty(WorkingDir) ? Path.GetDirectoryName(si.FileName) : WorkingDir;
            si.WindowStyle = ProcessWindowStyle.Hidden;
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.RedirectStandardInput = true;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;

            m_Process = new Process();
            m_Process.OutputDataReceived += ProcessOutputDataReceived;
            m_Process.ErrorDataReceived += ProcessErrorDataReceived;
            m_Process.Exited += ProcessExited;
            m_Process.StartInfo = si;

            try {
                m_Process.Start();
                m_Process.BeginOutputReadLine();
                m_Process.BeginErrorReadLine();
            } catch (Exception ex) {
                m_Process.Dispose();
                m_Process = null;
                throw ex;
            }
            string startOutput = await ReadOutput(string.Empty, TimeSpan.FromSeconds(1));

            // Empty command (gnuchess)
            await WriteCommand(string.Empty);

            if (startOutput.Contains("registration error"))
                await WriteCommand("register later");
            if (!string.IsNullOrEmpty(RegisterName) && !string.IsNullOrEmpty(RegisterCode))
                await Register();
            await WriteCommand("uci");
            ParseInfo(await ReadOutput("uciok"));

            if (m_Process.HasExited)
                throw new Exception("Failed to start engine");
            return true;
        } // Start

        public override async Task<bool> Stop()
        {
            if (m_Process != null) {
                await WriteCommand("quit");

                if (!m_Process.WaitForExit(1000))
                    m_Process.Kill();
                m_Process.Dispose();
                m_Process = null;
            }

            if (m_ProcessOutputSema != null) {
                m_ProcessOutputSema.WaitOne();
                m_ProcessOutputSema.Dispose();
                m_ProcessOutputSema = null;
            }
            if (m_ProcessInputSema != null) {
                m_ProcessInputSema.WaitOne();
                m_ProcessInputSema.Dispose();
                m_ProcessInputSema = null;
            }

            return true;
        } // Stop

        public async Task<bool> Register()
        {
            await WriteCommand($"register name {RegisterName} code {RegisterCode}");
            return true;
        } // Register

        public override bool SupportChess960()
        {
            return GetOption(Chess960OptionName) != null;
        } // SupportChess960

        public override Option GetChess960Option()
        {
            return GetOption(Uci.Chess960OptionName);
        } // GetChess960Option

        public override bool SupportAnalyze()
        {
            return GetOption(AnalyzeModeOptionName) != null;
        } // SupportAnalyze

        public override bool SupportPondering()
        {
            return GetOption(PonderOptionName) != null;
        } // SupportPondering

        public override bool IsPonderingEnabled()
        {
            return GetOption(PonderOptionName)?.Value == "true";
        } // IsPonderingEnabled

        public override void SetPondering(bool enabled)
        {
            var opt = GetOption(PonderOptionName);
            if (opt != null) {
                opt.Value = enabled ? "true" : "false";
            }
        } // SetPondering

        public override bool IsOwnBookEnabled()
        {
            return GetOption(OwnBookOptionName)?.Value == "true";
        } // IsOwnBookEnabled

        public override void SetOwnBook(bool enabled)
        {
            var opt = GetOption(OwnBookOptionName);
            if (opt != null) {
                opt.Value = enabled ? "true" : "false";
            }
        } // SetOwnBook

        public override int? GetElo()
        {
            if (GetOption(LimitStrengthOptionName)?.Value == "true") {
                int res;
                if (int.TryParse(GetOption(EloOptionName).Value, out res))
                    return res;
            }

            return null;
        } // GetElo

        public override bool CanSetElo()
        {
            return GetOption(LimitStrengthOptionName) != null && GetOption(EloOptionName) != null;
        } // CanSetElo

        public override bool SetElo(int elo)
        {
            var lsOpt = GetOption(LimitStrengthOptionName);
            var eloOpt = GetOption(EloOptionName);
            if (lsOpt != null && eloOpt != null) {
                int? min = !string.IsNullOrEmpty(eloOpt.Min) ? (int?)int.Parse(eloOpt.Min) : null;
                int? max = !string.IsNullOrEmpty(eloOpt.Max) ? (int?)int.Parse(eloOpt.Max) : null;

                if ((!min.HasValue || elo >= min.Value) && (!max.HasValue || elo <= max.Value)) {
                    eloOpt.Value = elo.ToString(CultureInfo.InvariantCulture);
                    lsOpt.Value = "true";
                }
                return true;
            }
            return false;
        } // SetElo

        public override int GetMinElo()
        {
            var eloOpt = GetOption(EloOptionName);
            if (eloOpt != null) {
                int? min = !string.IsNullOrEmpty(eloOpt.Min) ? (int?)int.Parse(eloOpt.Min) : null;
                return min ?? 0;
            }
            return 0;
        } // GetMinElo

        public override int GetMaxElo()
        {
            var eloOpt = GetOption(EloOptionName);
            if (eloOpt != null) {
                int? max = !string.IsNullOrEmpty(eloOpt.Max) ? (int?)int.Parse(eloOpt.Max) : null;
                return max ?? 0;
            }
            return 0;
        } // GetMaxElo

        public override async Task<bool> ApplyOptions(bool onlyModified)
        {
            // Set Threads option first
            // https://github.com/official-stockfish/Stockfish
            var thOpt = GetOption("Threads");
            if (thOpt != null && (!onlyModified || thOpt.Value != thOpt.Default))
                await SetOption(thOpt.Name, thOpt.Value);

            foreach (var opt in Options) {
                if (thOpt != null && opt == thOpt)
                    continue;

                if (!opt.Internal && (!onlyModified || opt.Value != opt.Default)) {
                    await SetOption(opt.Name, opt.Value);
                }
            }
            await IsReadyCommand();
            return true;
        } // ApplyOptions

        public override async Task<bool> SetOption(string name, string value)
        {
            var opt = GetOption(name);
            if (opt != null) {
                if (opt.Type == "check" && value != "true" && value != "false") {
                    throw new Exception($"Invalid value for option {name}");
                } else if (opt.Type == "spin") {
                    int iValue;
                    if (!int.TryParse(value, out iValue))
                        throw new Exception($"Invalid value for option {name}");
                    if (iValue < int.Parse(opt.Min) || iValue > int.Parse(opt.Max))
                        throw new Exception($"Invalid value for option {name}");
                } else if (opt.Type == "combo") {
                    if (!opt.ValidValues.Contains(value))
                        throw new Exception($"Invalid value for option {name}");
                }

                opt.Value = value;
                await WriteCommand($"setoption name {name} value {value}");
            } else
                throw new Exception($"Invalid option {name}");
            return true;
        } // SetOption

        public override async Task<bool> NewGame(int maxMinutes, int incrementSeconds)
        {
            await WriteCommand("ucinewgame");
            return true;
        } // NewGame

        public override Task<bool> ForceMove(string move)
        {
            return Task.FromResult(true);
        } // ForceMove

        public override async Task<bool> SetPosition(string fen, List<string> moves)
        {
            if (string.IsNullOrEmpty(fen))
                await WriteCommand($"position startpos moves {string.Join(" ", moves).ToLower()}");
            else
                await WriteCommand($"position fen {fen} moves {string.Join(" ", moves).ToLower()}");

            return true;
        } // SetPosition

        /// <summary>
        /// Start pondering the next move.
        /// You need to call <see cref="SetPosition(string, List{string})"/> with the pondered move returned by <see cref="GetBestMove(int, int, int, int, int?, TimeSpan?, Action{string})"/>
        /// before calling this method.
        /// If the other player makes the pondered move you should call <see cref="Ponderhit"/>.
        /// If the other player makes a different move you should call <see cref="SetPosition(string, List{string})"/> with the actual player move.
        /// </summary>
        /// <param name="whiteTimeLeft"></param>
        /// <param name="whiteIncrement"></param>
        /// <param name="blackTimeLeft"></param>
        /// <param name="blackIncrement"></param>
        /// <param name="depth"></param>
        /// <returns>The <see cref="BestMove"/></returns>
        public override async Task<BestMove> Ponder(int whiteTimeLeft, int whiteIncrement,
            int blackTimeLeft, int blackIncrement, int? depth, CancellationToken token,
            Action<string> outputCallback = null)
        {
            return await GetBestMovePrimitive(true, whiteTimeLeft, whiteIncrement, blackTimeLeft, blackIncrement, depth, null, token, null, outputCallback);
        } // Ponder

        /// <summary>
        /// Get the engine best move
        /// </summary>
        /// <param name="whiteTimeLeft"></param>
        /// <param name="whiteIncrement"></param>
        /// <param name="blackTimeLeft"></param>
        /// <param name="blackIncrement"></param>
        /// <param name="depth"></param>
        /// <param name="maxTimeSpan"></param>
        /// <param name="outputCallback"></param>
        /// <returns>The <see cref="BestMove"/></returns>
        public override async Task<BestMove> GetBestMove(int whiteTimeLeft, int whiteIncrement,
            int blackTimeLeft, int blackIncrement,
            int? depth, TimeSpan? maxTimeSpan,
            CancellationToken token,
            List<string> searchMoves = null,
            Action<string> outputCallback = null)
        {
            return await GetBestMovePrimitive(false, whiteTimeLeft, whiteIncrement, blackTimeLeft, blackIncrement,
                depth, maxTimeSpan, token, searchMoves, outputCallback);
        } // GetBestMove

        public override int GetDefaultAnalyzeDepth()
        {
            return 10;
        } // GetDefaultAnalyzeDepth

        public override async Task<bool> EnterAnalyzeMode()
        {
            await SetOption(AnalyzeModeOptionName, "true");
            await IsReadyCommand();
            return true;
        } // EnterAnalyzeMode

        public override async Task<bool> ExitAnalyzeMode()
        {
            await SetOption(AnalyzeModeOptionName, "false");
            await IsReadyCommand();
            return true;
        } // ExitAnalyzeMode

        public override async Task<AnalyzeResult> Analyze(string fen, int? depth, TimeSpan? maxTimeSpan, CancellationToken token)
        {
            string[] parts = fen?.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts != null) {
                var res = new AnalyzeResult(parts[1] == "w" ? Game.Colors.White : Game.Colors.Black, new Info.ScoreValue());
                await SetPosition(fen, new List<string>());
                await GetBestMovePrimitive(false, 0, 0, 0, 0,
                    depth, maxTimeSpan, token, null,
                    (output) =>
                    {
                        var info = ParseThinkingInfo(output);
                        if (info != null)
                            res.Score = info.Score;
                    });
                return res;
            }
            return null;
        } // Analyze

        public override async Task<bool> StopCommand()
        {
            await WriteCommand("stop");
            return true;
        } // StopCommand

        public override async Task<bool> Ponderhit()
        {
            await WriteCommand("ponderhit");
            return true;
        } // Ponderhit

        /// <summary>
        /// Get a <see cref="ThinkingInfo" /> from the engine output
        /// </summary>
        /// <param name="output">The engine output</param>
        /// <returns>A <see cref="ThinkingInfo" /> or null if the output does not contain any score</returns>
        public override Info ParseThinkingInfo(string output)
        {
            Info res = null;
            int value = 0;

            List<string> parts = output.Replace("\t", string.Empty).ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            int idx = parts.IndexOf("score");
            if (idx >= 0) {
                res = new Info();

                if (parts[idx + 1] == "cp" && int.TryParse(parts[idx + 2], out value))
                    res.Score = new Info.ScoreValue() { CentiPawns = value };
                else if (parts[idx + 1] == "mate" && int.TryParse(parts[idx + 2], out value))
                    res.Score = new Info.ScoreValue() { MateIn = value };

                idx = parts.IndexOf("depth");
                if (idx >= 0) {
                    if (int.TryParse(parts[idx + 1], out value))
                        res.Depth = value;
                }

                idx = parts.IndexOf("seldepth");
                if (idx >= 0) {
                    if (int.TryParse(parts[idx + 1], out value))
                        res.SelDepth = value;
                }

                idx = parts.IndexOf("nodes");
                if (idx >= 0) {
                    if (int.TryParse(parts[idx + 1], out value))
                        res.Nodes = value;
                }

                idx = parts.IndexOf("nps");
                if (idx >= 0) {
                    if (int.TryParse(parts[idx + 1], out value))
                        res.NodesPerSecond = value;
                }

                idx = parts.IndexOf("time");
                if (idx >= 0) {
                    if (int.TryParse(parts[idx + 1], out value))
                        res.Time = value;
                }

                idx = parts.IndexOf("multipv");
                if (idx >= 0) {
                    if (int.TryParse(parts[idx + 1], out value))
                        res.MultiPv = value;
                }

                idx = parts.IndexOf("currmove");
                if (idx >= 0)
                    res.CurrMove = parts[idx + 1];

                idx = parts.IndexOf("pv");
                if (idx >= 0) {
                    res.Pv = new List<string>();
                    for (int i = idx + 1; i < parts.Count; i++) {
                        res.Pv.Add(parts[i]);
                    }
                }
            }

            return res;
        } // ParseThinkingInfo

        #region private operations
        private async Task<bool> IsReadyCommand()
        {
            await WriteCommand("isready");
            await ReadOutput("readyok");
            return true;
        } // IsReadyCommand

        private async Task<BestMove> GetBestMovePrimitive(bool ponder,
        int whiteTimeLeft, int whiteIncrement,
            int blackTimeLeft, int blackIncrement,
            int? depth, TimeSpan? maxTimeSpan,
            CancellationToken token,
            List<string> searchMoves = null,
            Action<string> outputCallback = null)
        {
            string cmd = "go";
            if (ponder)
                cmd = $"{cmd} ponder";
            if (whiteTimeLeft > 0)
                cmd = $"{cmd} wtime {whiteTimeLeft}";
            if (blackTimeLeft > 0)
                cmd = $"{cmd} btime {blackTimeLeft}";
            if (whiteIncrement > 0)
                cmd = $"{cmd} winc {whiteIncrement}";
            if (blackIncrement > 0)
                cmd = $"{cmd} binc {blackIncrement}";
            if (depth.HasValue)
                cmd = $"{cmd} depth {depth}";

            if (searchMoves != null && searchMoves.Count > 0)
                cmd = $"{cmd} searchmoves {string.Join(" ", searchMoves)}";

            await WriteCommand(cmd);

            var started = DateTime.UtcNow;
            while (true) {
                if (token.IsCancellationRequested || maxTimeSpan.HasValue && (DateTime.UtcNow - started).TotalMilliseconds >= maxTimeSpan.Value.TotalMilliseconds)
                    await StopCommand();

                string output = await ReadOutput();
                if (outputCallback != null) {
                    string tOutput = output.Replace("\r", string.Empty);
                    foreach (string line in tOutput.Split('\n'))
                        outputCallback.Invoke(line);
                }

                int idx = output.IndexOf("bestmove ");
                if (idx >= 0) {
                    string move = output.Substring(idx + 9);
                    string ponderMove = string.Empty;
                    idx = move.IndexOf(" ");
                    if (idx >= 0)
                        move = move.Substring(0, idx);

                    // Check ponder
                    idx = output.IndexOf("ponder ");
                    if (idx > 0)
                        ponderMove = output.Substring(idx + 7);

                    if (token.IsCancellationRequested)
                        return null;
                    return new BestMove(move.Trim(), ponderMove.Trim());
                }
                await Task.Delay(100);
            }
        } // GetBestMovePrimitive

        private async Task<bool> WriteCommand(string command)
        {
            m_ProcessInputSema.WaitOne();
            if (m_Process.HasExited) {
                m_ProcessInputSema.Release();
                return false;
            }

            Debug.WriteLine($"UCI command: {command}");
            if (!command.EndsWith("\n"))
                command = $"{command}\n";

            await m_Process.StandardInput.WriteAsync(command);
            await m_Process.StandardInput.FlushAsync();
            m_ProcessInputSema.Release();

            return true;
        } // WriteCommand

        private async Task<string> ReadOutput(string commandEnd = null, TimeSpan? timeout = null)
        {
            string res = string.Empty;
            DateTime started = DateTime.UtcNow;
            while (m_Process != null && !m_Process.HasExited) {
                m_ProcessOutputSema.WaitOne();
                res = m_ProcessOutput.ToString();
                if (commandEnd == null && res.Length > 0 || commandEnd != null && res.TrimEnd().EndsWith(commandEnd)) {
                    m_ProcessOutput.Clear();
                    m_ProcessOutputSema.Release();
                    break;
                }
                m_ProcessOutputSema.Release();
                await Task.Delay(100);
                if (timeout.HasValue && (DateTime.UtcNow - started) > timeout)
                    break;
            }
            return res;
        } // ReadOutput

        private void ParseInfo(string output)
        {
            if (string.IsNullOrEmpty(output))
                return;

            output = output.Replace("\r", string.Empty);
            Regex regex = new Regex("option name (.*?) type (.*?) (.*)?");

            foreach (string line in output.Split(new char[] { '\n' })) {
                if (line.StartsWith("id name ")) {
                    if (string.IsNullOrEmpty(Name))
                        Name = line.Substring(8);
                } else if (line.StartsWith("id author ")) {
                    Author = line.Substring(10);
                } else if (line.StartsWith("option ")) {
                    var match = regex.Match(line);
                    if (match.Success) {
                        var option = new Option()
                        {
                            Name = match.Groups[1].Value,
                            Type = match.Groups[2].Value,
                        };

                        if (option.Type == "check") {
                            var m = Regex.Match(match.Groups[3].Value, "default (.*)");
                            if (m.Success)
                                option.Default = m.Groups[1].Value;
                        } else if (option.Type == "spin") {
                            var m = Regex.Match(match.Groups[3].Value, "default (.*) min (.*) max (.*)");
                            if (m.Success) {
                                option.Default = m.Groups[1].Value;
                                option.Min = m.Groups[2].Value;
                                option.Max = m.Groups[3].Value;
                            }
                        } else if (option.Type == "combo") {
                            var m = Regex.Match(match.Groups[3].Value, "default (.*)");
                            if (m.Success) {
                                string[] parts = Regex.Split(m.Groups[1].Value, "(^| )var ");
                                option.Default = parts[0];

                                for (int i = 1; i < parts.Length; i++) {
                                    var p = parts[i];
                                    if (!string.IsNullOrEmpty(p?.Trim()))
                                        option.ValidValues.Add(p);
                                }
                            }
                        } else if (option.Type == "button") {
                        } else if (option.Type == "string") {
                            var m = Regex.Match(match.Groups[3].Value, "default (.*)");
                            if (m.Success)
                                option.Default = m.Groups[1].Value;
                        }

                        option.Value = option.Default;
                        var exOpt = GetOption(option.Name);
                        if (exOpt == null)
                            Options.Add(option);
                        else
                            exOpt.Default = option.Default;
                    }
                }
            }
        } // ParseInfo
        #endregion

        #region process events
        private void ProcessExited(object sender, EventArgs e)
        {
            Debug.WriteLine($"UCI engine exited with code: {m_Process.ExitCode}");
        } // ProcessExited

        private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine($"UCI engine error: {e.Data}");
        } // ProcessErrorDataReceived

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            m_ProcessOutputSema.WaitOne();
            Debug.WriteLine($"UCI engine output: {e.Data}");
            if (e.Data == null && m_EngineError.Length > 0) {
                RaiseError(m_EngineError.ToString());
                m_EngineError.Clear();
            }
            if (e.Data != null && e.Data.StartsWith("info string ERROR: ")) {
                var error = e.Data.Substring(19);
                m_EngineError.AppendLine(error);
            }
            m_ProcessOutput.AppendLine(e.Data);
            m_ProcessOutputSema.Release();
        } // ProcessOutputDataReceived
        #endregion
    } // Engine
}
