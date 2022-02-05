using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChessLib
{
    /// <summary>
    /// PGN file format
    /// https://opensource.apple.com/source/Chess/Chess-109.0.3/Documentation/PGN-Standard.txt.auto.html
    /// </summary>
    public class PGN
    {
        public class Move
        {
            public string Notation { get; set; }
            public string Comment { get; set; }
        } // Move

        public PGN()
        {
            Moves = new List<Move>();
        }

        public string Event { get; set; }
        public string Site { get; set; }
        public string Date { get; set; }
        public string Round { get; set; }
        public string White { get; set; }
        public int? WhiteElo { get; set; }
        public string Black { get; set; }
        public int? BlackElo { get; set; }
        public string Result { get; set; }
        public List<Move> Moves { get; set; }

        public string ECO { get; set; }
        public string Termination{ get; set; }
        public TimeSpan? Time { get; set; }
        public string SetUp 
        {
            get { return string.IsNullOrEmpty(FEN) ? "0" : "1"; }
        }
        public string FEN { get; set; }
        public string Annotator { get; set; }
        public string PlyCount { get; set; }
        public string TimeControl { get; set; }
        public string Mode { get; set; }

        public DateTime? GetDate()
        {
            DateTime? res = null;

            if (!string.IsNullOrEmpty(Date)) {
                string[] parts = Date.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3) {
                    int year = 0;
                    int month = 1;
                    int day = 1;
                    if (parts[0] != "??")
                        int.TryParse(parts[0], out year);
                    if (parts[1] != "??")
                        int.TryParse(parts[1], out month);
                    if (parts[2] != "??")
                        int.TryParse(parts[1], out day);

                    res = new DateTime(year, month, day);
                }
            }
            return res;
        }

        /// <summary>
        /// Save PGN to file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<bool> Save(string file)
        {
            using (StreamWriter sw = new StreamWriter(file)) {
                await sw.WriteLineAsync($"[Event \"{ Event }\"]");
                await sw.WriteLineAsync($"[Site \"{ Site }\"]");
                await sw.WriteLineAsync($"[Date \"{ (Date == null ? "??" : Date) }\"]");
                await sw.WriteLineAsync($"[Round \"{(string.IsNullOrEmpty(Round) ? "?" : Round)}\"]");
                await sw.WriteLineAsync($"[White \"{ White }\"]");
                if (WhiteElo.HasValue)
                    await sw.WriteLineAsync($"[WhiteElo \"{ WhiteElo }\"]");
                await sw.WriteLineAsync($"[Black \"{ Black }\"]");
                if (BlackElo.HasValue)
                    await sw.WriteLineAsync($"[BlackElo \"{ BlackElo }\"]");                
                await sw.WriteLineAsync($"[Result \"{ Result }\"]");

                if (!string.IsNullOrEmpty(Termination))
                    await sw.WriteLineAsync($"[Termination \"{ Termination }\"]");

                if (!string.IsNullOrEmpty(ECO))
                    await sw.WriteLineAsync($"[ECO \"{ ECO }\"]");
                await sw.WriteLineAsync($"[SetUp \"{ SetUp }\"]");
                if (!string.IsNullOrEmpty(FEN))
                    await sw.WriteLineAsync($"[FEN \"{ FEN }\"]");
                if (!string.IsNullOrEmpty(Annotator))
                    await sw.WriteLineAsync($"[Annotator \"{ Annotator }\"]");
                if (!string.IsNullOrEmpty(Annotator))
                    await sw.WriteLineAsync($"[PlyCount \"{ PlyCount }\"]");
                if (!string.IsNullOrEmpty(Annotator))
                    await sw.WriteLineAsync($"[Mode \"{ Mode }\"]");
                if (Time != null)
                    await sw.WriteLineAsync($"[Time \"{ Time.Value.ToString(@"HH\:mm\:ss") }\"]");

                // Moves
                await sw.WriteLineAsync();
                int moveNumber = 1;
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < Moves.Count; i++) {
                    string move = string.Empty;
                    if (i % 2 == 0) {
                        if (moveNumber > 1)
                            move = $" {  moveNumber++ }.";
                        else
                            move = $"{  moveNumber++ }.";
                    }

                    if (Moves[i].Notation == "0-0")
                        move = $"{move} O-O";
                    else if (Moves[i].Notation == "0-0-0")
                        move = $"{move} O-O-O";
                    else
                        move = $"{move} {Moves[i].Notation}";

                    sb.Append(move);
                    if (!string.IsNullOrEmpty(Moves[i].Comment))
                        sb.Append($" {{{Moves[i].Comment}}}");

                    if (sb.Length >= 80) {
                        await sw.WriteLineAsync(sb.ToString().Trim());
                        sb.Clear();
                    }
                }
                if (sb.Length > 0)
                    await sw.WriteLineAsync(sb.ToString().Trim());

            }
            return true;
        } // Save

        /// <summary>
        /// Load all the games from a PGN stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static async Task<List<PGN>> LoadFromStream(Stream stream)
        {
            List<PGN> res = new List<PGN>();
            while (stream.Position < stream.Length) {
                var g = await LoadGamePrimitive(stream, stream.Position);
                res.Add(g);
            }
            return res;
        } // LoadFromStream

        /// <summary>
        /// Load all the games from a PGN file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static async Task<List<PGN>> LoadFile(string file)
        {
            using (Stream s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {                
                return await LoadFromStream(s);
            }
        } // LoadFile

        /// <summary>
        /// Load a game informations from a PGN file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="filePosition">The file position where the game informations start</param>
        /// <returns></returns>
        public static async Task<PGN> LoadGame(string file, long filePosition = 0)
        {
            using (Stream s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                return await LoadGamePrimitive(s, filePosition);
            }
        } //LoadGame

        #region private operations        
        /// <summary>
        /// Load a game informations from a PGN file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="filePosition">The file position where the game informations start</param>
        /// <returns></returns>
        private static async Task<PGN> LoadGamePrimitive(Stream stream, long filePosition = 0)
        {
            PGN res = new PGN();

            stream.Seek(filePosition, SeekOrigin.Begin);

            bool movesStarted = false;
            string line = null;
            long lastPos = filePosition;
            StringBuilder sb = new StringBuilder();
            Regex propsRegEx = new Regex("\\[(.*) \"(.*)\"\\]");
            while ((line = await ReadLineFromStream(stream)) != null) {
                line = line.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("%"))
                    continue;

                if (line.StartsWith("[")) {
                    if (movesStarted) {
                        stream.Position = lastPos;
                        break;
                    }
                    
                    var m = propsRegEx.Match(line);
                    if (m.Success) {
                        string name = m.Groups[1].Value.Trim();
                        string value = m.Groups[2].Value.Trim();

                        if (name == "Event") {
                            res.Event = value;
                        } else if (name == "Site") {
                            res.Site = value;
                        } else if (name == "Date") {
                            if (value != "??")
                                res.Date = value;
                        } else if (name == "Time") {
                            TimeSpan time;
                            if (TimeSpan.TryParseExact(value, @"HH\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.TimeSpanStyles.None, out time))
                                res.Time = time;
                        } else if (name == "Round") {
                            res.Round = value;
                        } else if (name == "White") {
                            res.White = value;
                        } else if (name == "WhiteElo") {
                            int elo;
                            if (int.TryParse(value, out elo))
                                res.WhiteElo = elo;                            
                        } else if (name == "Black") {
                            res.Black = value;
                        } else if (name == "BlackElo") {
                            int elo;
                            if (int.TryParse(value, out elo))
                                res.BlackElo = elo;                               
                        } else if (name == "Result") {
                            res.Result = value;
                        } else if (name == "Termination") {
                            res.Termination = value;
                        } else if (name == "FEN") {
                            res.FEN = value;
                        } else if (name == "Annotator") {
                            res.Annotator = value;
                        } else if (name == "PlyCount") {
                            res.PlyCount = value;
                        } else if (name == "TimeControl") {
                            res.TimeControl = value;
                        } else if (name == "Mode") {
                            res.Mode = value;
                        } else if (name == "ECO") {
                            res.ECO = value;                            
                        }
                    }
                } else {
                    // Move line
                    movesStarted = true;

                    // Rest of line comment
                    int idx = line.IndexOf(";");
                    if (idx >= 0) { 
                        line = line.Remove(idx, 1);
                        line.Insert(idx, "{");
                        line.Insert(line.Length - 1, "}");
                    }
                    sb.Append(line);
                    sb.Append(" ");
                }
                lastPos = stream.Position;
            }

            //Moves
            string moves = sb.ToString();
            moves = moves.Trim();
            if (moves.EndsWith(" 1-0") || moves.EndsWith(" 0-1"))
                moves = moves.Remove(moves.Length - 4, 4);
            else if (moves.EndsWith(" 1/2-1/2"))
                moves = moves.Remove(moves.Length - 8, 8);
            moves = moves.Trim();

            int moveIdx = 0;
            var moveMatch = Regex.Match(moves, "([0-9]+\\.) ?([^ ]+) ?({[^{]+})? ?([^ ]+)? ?({[^{]+})?");
            while (moveMatch.Success) {
                if (moveMatch.Groups.Count < 4)
                    throw new Exception("Failed to load file");

                int number;
                if (!int.TryParse(moveMatch.Groups[1].Value.Remove(moveMatch.Groups[1].Value.Length - 1, 1), out number))
                    throw new Exception("Failed to load file");

                if (number != ++moveIdx)
                    throw new Exception("Failed to load file");

                res.Moves.Add(new Move()
                {
                    Notation = moveMatch.Groups[2].Value,
                    Comment = GetComment(moveMatch.Groups[3].Value),
                });

                if (moveMatch.Groups.Count > 4 && !string.IsNullOrEmpty(moveMatch.Groups[4].Value.Trim())) {
                    res.Moves.Add(new Move()
                    {
                        Notation = moveMatch.Groups[4].Value.Trim(),
                        Comment = GetComment(moveMatch.Groups[5].Value),
                    });
                }
                moveMatch = moveMatch.NextMatch();
            }
            return res;
        } // LoadGamePrimitive

        private static string GetComment(string comment)
        {
            if (!string.IsNullOrEmpty(comment)) {
                if (comment.StartsWith("{"))
                    comment = comment.Remove(0, 1);
                if (comment.EndsWith("}"))
                    comment = comment.Remove(comment.Length - 1, 1);

            }
            return comment;
        } // GetComment

        private static async Task<string> ReadLineFromStream(Stream s)
        {
            long filePos = s.Position;
            byte[] buffer = new byte[80];
            List<byte> bytes = new List<byte>();
            while (true) {
                int read = await s.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0) {
                    if (bytes.Count == 0)
                        return null;
                    return Encoding.UTF8.GetString(bytes.ToArray());
                }

                for (int i = 0; i < read; i++) {
                    filePos++;
                    if (buffer[i] == '\n') {
                        s.Seek(filePos, SeekOrigin.Begin);
                        return Encoding.UTF8.GetString(bytes.ToArray());
                    } else if (buffer[i] != '\r') { 
                        bytes.Add(buffer[i]);
                    }
                }
            }
        } // ReadLineFromStream
        #endregion
    }
}
