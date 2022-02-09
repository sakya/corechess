using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChessLib.Engines
{
    /// <summary>
    /// CECP chess engine
    /// https://www.gnu.org/software/xboard/engine-intf.html
    /// </summary>
    public class Cecp : EngineBase
    {
        public const string PonderOptionName = "Ponder";

        private StringBuilder m_ProcessOutput = new StringBuilder();
        private Semaphore m_ProcessOutputSema = null;
        private Semaphore m_ProcessInputSema = null;
        private Process m_Process = null;
        private Random m_Rnd = new Random();

        public Cecp(string name, string command)
            : base(name, command)
        {
            Features = new List<Option>();
        }

        public int ProtocolVersion { get; set; }
        public List<Option> Features { get; set; }

        public Option GetFeature(string name)
        {
            return Features?.Where(f => f.Name == name).FirstOrDefault();
        } // GetFeature

        public override int GetDefaultAnalyzeDepth()
        {
            return 10;
        } // GetDefaultAnalyzeDepth

        public override async Task<bool> EnterAnalyzeMode()
        {
            await WriteCommand("analyze");
            await WaitPong();
            return true;
        } // EnterAnalyzeMode

        public override async Task<bool> ExitAnalyzeMode()
        {
            await WriteCommand("exit");
            await WaitPong();
            return true;
        } // ExitAnalyzeMode

        public override async Task<AnalyzeResult> Analyze(string fen, int? depth, TimeSpan? maxTimeSpan, CancellationToken token)
        {
            string[] parts = fen.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var res = new AnalyzeResult(parts[1] == "w" ? Game.Colors.White : Game.Colors.Black, new Info.ScoreValue());
            await SetBoard(fen);
            var started = DateTime.UtcNow;
            bool done = false;
            while (!done) {
                if (token.IsCancellationRequested || maxTimeSpan.HasValue && (DateTime.UtcNow - started).TotalMilliseconds >= maxTimeSpan.Value.TotalMilliseconds)
                    break;

                string output = await ReadOutput();
                output = output.Replace("\r", string.Empty);
                string[] lines = output.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines) {
                    var info = ParseThinkingInfo(line);
                    if (info != null) {
                        res.Score = info.Score;

                        if (depth.HasValue && info.Depth >= depth || res.Score.CentiPawns > 9000 || res.Score.CentiPawns < -9000)
                            done = true;
                    }
                }
                await Task.Delay(100);
            }

            return res;
        } // Analyze

        public override async Task<bool> ApplyOptions(bool onlyModified)
        {
            foreach (var opt in Options) {
                if (opt.Name == PonderOptionName) {
                    if (opt.Value == "true")
                        await WriteCommand("hard");
                    else
                        await WriteCommand("easy");
                } else if (!opt.Internal)
                    await SetOption(opt.Name, opt.Value);
            }
            await WaitPong();
            return true;
        }

        public override async Task<BestMove> GetBestMove(int whiteTimeLeft, int whiteIncrement, int blackTimeLeft,
            int blackIncrement, int? depth, TimeSpan? maxTimeSpan,
            CancellationToken token,
            List<string> searchMoves = null, Action<string> outputCallback = null)
        {
            if (PlayAs == Game.Colors.White) {
                await WriteCommand($"time {whiteTimeLeft / 10}");
                await WriteCommand($"otim {blackTimeLeft / 10}");
            } else {
                await WriteCommand($"time {blackTimeLeft / 10}");
                await WriteCommand($"otim {whiteTimeLeft / 10}");
            }

            await WriteCommand($"go");
            var started = DateTime.UtcNow;

            while (true) {
                if (token.IsCancellationRequested || maxTimeSpan.HasValue && (DateTime.UtcNow - started).TotalMilliseconds >= maxTimeSpan.Value.TotalMilliseconds)
                    await WriteCommand("force");

                string output = await ReadOutput();
                output = output.Replace("\r", string.Empty);
                string[] lines = output.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++) {
                    var line = lines[i];
                    if (line.StartsWith("move ")) {
                        if (i < lines.Length - 1 && lines[i + 1].StartsWith("Hint:"))
                            return new BestMove(line.Substring(5), lines[i + 1].Substring(5));
                        else
                            return new BestMove(line.Substring(5), string.Empty);
                    }
                    outputCallback?.Invoke(line);
                }
                await Task.Delay(100);
            }
        } // GetBestMove

        public override Option GetChess960Option()
        {
            return null;
        }

        public override Info ParseThinkingInfo(string output)
        {
            Info res = null;
            int depth = 0;
            int score = 0;
            int nodes = 0;
            int time = 0;

            string[] parts = output.Replace("\t", string.Empty).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 4) {
                if (int.TryParse(parts[0], out depth)) {
                    int.TryParse(parts[1], out score);
                    int.TryParse(parts[2], out time);
                    int.TryParse(parts[3], out nodes);

                    res = new Info()
                    {
                        Depth = depth,
                        Score = new Info.ScoreValue() { CentiPawns = score },
                        Time = time,
                        Nodes = nodes,
                        Pv = new List<string>()
                    };

                    for (int i = 4; i < parts.Length; i++)
                        res.Pv.Add(parts[i]);
                }
            }
            return res;
        } // ParseThinkingInfo

        public override async Task<bool> NewGame(int maxMinutes, int incrementSeconds)
        {
            await WriteCommand("new");
            if (maxMinutes > 0)
                await WriteCommand($"level {0} {maxMinutes} {incrementSeconds}");
            await WriteCommand("post");
            return true;
        }

        public override Task<BestMove> Ponder(int whiteTimeLeft, int whiteIncrement, int blackTimeLeft, int blackIncrement, int? depth,
            CancellationToken token, Action<string> outputCallback = null)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> Ponderhit()
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> SetOption(string name, string value)
        {
            await WriteCommand($"{name.ToLower()} {value}");
            return true;
        }

        public override Task<bool> SetPosition(string fen, List<string> moves)
        {
            return Task.FromResult(true);
        } // SetPosition

        public async Task<bool> SetBoard(string fen)
        {
            if (ProtocolVersion == 2 && GetFeature("setboard")?.Value != "1")
                return false;

            await WriteCommand("force");
            await WriteCommand($"setboard {fen}");
            return true;
        } // SetBoard

        public override async Task<bool> ForceMove(string move)
        {
            await WriteCommand("force");
            await WriteCommand($"{move}");

            return true;
        } // ForceMove

        public override async Task<bool> Start()
        {
            await Stop();

            m_ProcessOutputSema = new Semaphore(1, 1);
            m_ProcessInputSema = new Semaphore(1, 1);

            ProcessStartInfo si = new ProcessStartInfo();
            si.FileName = si.FileName = GetExePath(Command);
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

            await WriteCommand("xboard");
            await WriteCommand("protover 2");
            ParseFeatures(await ReadOutput("done=1", TimeSpan.FromSeconds(1)));
            ProtocolVersion = Features.Count > 0 ? 2 : 1;

            if (m_Process.HasExited)
                throw new Exception("Failed to start engine");
            return true;
        }

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
        }

        public override async Task<bool> StopCommand()
        {
            await WriteCommand("force");
            return true;
        } // StopCommand

        public override bool SupportAnalyze()
        {
            if (ProtocolVersion == 2 && GetFeature("analyze")?.Value != "1")
                return false;
            return true;
        }

        public override bool SupportChess960()
        {
            return false;
        }

        public override bool SupportPondering()
        {
            return true;
        }

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
            return false;
        }

        public override void SetOwnBook(bool enabled)
        {
        } // SetOwnBook

        public override int? GetElo() {
            return null;
        } // GetElo

        public override bool CanSetElo()
        {
            return false;
        } // CanSetElo

        public override bool SetElo(int elo)
        {
            return false;
        }

        public override int GetMinElo()
        {
            return 0;
        }

        public override int GetMaxElo()
        {
            return 0;
        }
        #region process events
        private void ProcessExited(object sender, EventArgs e)
        {
            Debug.WriteLine($"CECP engine exited with code: {m_Process.ExitCode}");
        } // ProcessExited

        private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine($"CECP engine error: {e.Data}");
        } // ProcessErrorDataReceived

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            m_ProcessOutputSema.WaitOne();
            Debug.WriteLine($"CECP engine output: {e.Data}");
            if (e.Data != null && !e.Data.StartsWith("tell") && !e.Data.StartsWith("ask") && !e.Data.StartsWith("#"))
                m_ProcessOutput.AppendLine(e.Data);
            m_ProcessOutputSema.Release();
        } // ProcessOutputDataReceived
        #endregion

        #region private operations
        protected async Task<bool> WriteCommand(string command)
        {
            m_ProcessInputSema.WaitOne();
            Debug.WriteLine($"CECP command: {command}");
            if (!command.EndsWith("\n"))
                command = $"{command}\n";

            await m_Process.StandardInput.WriteAsync(command);
            await m_Process.StandardInput.FlushAsync();
            m_ProcessInputSema.Release();
            return true;
        } // WriteCommand

        protected async Task<string> ReadOutput(string commandEnd = null, TimeSpan? timeout = null)
        {
            string res = string.Empty;
            DateTime started = DateTime.UtcNow;
            while (m_Process != null && !m_Process.HasExited) {
                m_ProcessOutputSema.WaitOne();
                res = m_ProcessOutput.ToString();
                if (commandEnd == null && res.Length > 0 || commandEnd != null && res.TrimEnd().EndsWith(commandEnd))
                {
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

        private void ParseFeatures(string output)
        {
            if (string.IsNullOrEmpty(output))
                return;

            output = output.Replace("\r", string.Empty);

            Regex r = new Regex("([^ ]+)=(\".*? \"|[^ ]+)");
            foreach (var line in output.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
                if (line.StartsWith("feature ")) {
                    var m = r.Match(line);
                    while (m.Success) {
                        string name = m.Groups[1].Value;
                        string value = m.Groups[2].Value;
                        if (value.StartsWith("\"") && value.EndsWith("\"")) {
                            value = value.Remove(0, 1);
                            value = value.Remove(value.Length - 1, 1);
                        }

                        if (name == "myname") {
                            if (string.IsNullOrEmpty(Name))
                                Name = value;
                        } else if (name == "done") {
                            if (value == "1") {
                                ParseOptions();
                                return;
                            }
                        } else {
                            Features.Add(new Option() { Name = name, Value = value });
                        }
                        m = m.NextMatch();
                    }
                }
            }

            ParseOptions();
        } // ParseFeatures

        protected virtual void ParseOptions()
        {
            if (Options.Count == 0) {
                Options.Add(new Option() { Name = PonderOptionName, Type = "check", Default = "true", Value = "true" });

                var tempFeature = Features.Where(f => f.Name == "memory").FirstOrDefault();
                if (tempFeature?.Value == "1")
                    Options.Add(new Option() { Name = "Memory", Type = "spin", Min = "0", Max = "1024", Default = "1", Value = "1" });

                tempFeature = Features.Where(f => f.Name == "smp").FirstOrDefault();
                if (tempFeature?.Value == "1")
                    Options.Add(new Option() { Name = "Cores", Type = "spin", Min = "1", Max = "16", Default = "1", Value = "1" });
            }
        } // ParseOptions

        protected virtual async Task<bool> WaitPong()
        {
            int number = m_Rnd.Next(0, 99999);
            await WriteCommand($"ping {number}");
            await ReadOutput($"pong {number}");

            return true;
        } // WaitPong
        #endregion
    }
}