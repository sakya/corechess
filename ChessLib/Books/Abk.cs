using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChessLib.Books
{
    public class Abk : IBook
    {
        #region classes
        public class Entry : IBookEntry
        {
            public string Move { get; set; }

            public int Priority { get; set; }
            public int NumberOfGames { get; set; }
            public int NumberOfWins { get; set; }
            public int NumberOfLosses { get; set; }
            public int PlyCount { get; set; }
            public int NextMovePointer  { get; set; }
            public int NextSiblingPointer { get; set; }

            public string GetMove()
            {
                return Move;
            } // GetMove

            public int GetPriority()
            {
                return Priority;
            }
        } // Entry
        #endregion

        private const int MovesStart = 25200;
        private const int MoveLength = 28;

        private Dictionary<string, long> m_Index = new Dictionary<string, long>();

        public Abk()
        {
        }

        public string FileName { get; set; }
        public string Comment { get; set; }
        public string Author { get; set; }
        public int DepthInHalfMoves { get; set; }
        public int MovesCount { get; set; }
        public int MinimumNumberOfGames { get; set; }
        public int MinimumNumberOfWins { get; set; }
        public int WinWhitePercentage { get; set; }
        public int WinBlackPercentage { get; set; }
        public int ProbabilityPriority { get; set; }
        public int ProbabilityNumberOfGames { get; set; }
        public int ProbabilityWinPercentage { get; set; }
        public int UseBookToHalfMove { get; set; }

        public void Dispose()
        {
        }

        public List<IBookEntry> GetMovesFromFen(string fenString)
        {
            throw new NotImplementedException();
        } // GetMovesFromFen

        public List<IBookEntry> GetMovesFromMoves(List<string> moves)
        {
            long startVariation;
            if (!m_Index.TryGetValue(moves[0], out startVariation))
                return null;

            HashSet<string> resMoves = new HashSet<string>();
            List<Entry> res = new List<Entry>();
            using (FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                if (fs.Seek(startVariation, SeekOrigin.Begin) != startVariation)
                    return null;

                Entry entry = ReadEntry(fs);
                for (int i = 0; i < moves.Count; i++) {
                    if (entry.NextMovePointer <= 0)
                        return null;

                    long nsPos = entry.NextMovePointer * MoveLength;
                    if (fs.Seek(nsPos, SeekOrigin.Begin) != nsPos)
                        break;

                    while (true) {
                        entry = ReadEntry(fs);
                        if (i + 1 >= moves.Count || entry.GetMove() == moves[i + 1])
                            break;

                        if (entry.NextSiblingPointer <= 0)
                            return null;
                        nsPos = entry.NextSiblingPointer * MoveLength;
                        if (fs.Seek(nsPos, SeekOrigin.Begin) != nsPos)
                            break;
                    }
                }

                while (entry != null) {
                    if (!resMoves.Contains(entry.Move)) {
                        res.Add(entry);
                        resMoves.Add(entry.Move);
                    }

                    if (entry.NextSiblingPointer <= 0)
                        break;
                    long fsPos = entry.NextSiblingPointer * MoveLength;
                    if (fs.Seek(fsPos, SeekOrigin.Begin) != fsPos)
                        break;

                    entry = ReadEntry(fs);
                }
            }
            return res.OrderByDescending(m => m.Priority).ToList<IBookEntry>();
        } // GetMovesFromMoves

        public bool Open(string fileName)
        {
            if (!File.Exists(fileName))
                return false;

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                // Read the header
                byte[] buffer = new byte[254];
                if (fs.Read(buffer, 0, 254) != 254)
                    return false;

                if (buffer[0] != 0x03 || buffer[1] != 0x41 || buffer[2] != 0x42)
                    return false;

                int chars = (int)buffer[12];
                Comment = Encoding.UTF8.GetString(buffer, 13, chars);
                chars = (int)buffer[133];
                Author = Encoding.UTF8.GetString(buffer, 134, chars);

                DepthInHalfMoves = GetIntFromByteArray(buffer, 214);
                MovesCount = GetIntFromByteArray(buffer, 218);
                MinimumNumberOfGames = GetIntFromByteArray(buffer, 222);
                MinimumNumberOfWins = GetIntFromByteArray(buffer, 226);
                WinWhitePercentage = GetIntFromByteArray(buffer, 230);
                WinBlackPercentage = GetIntFromByteArray(buffer, 234);
                ProbabilityPriority = GetIntFromByteArray(buffer, 238);
                ProbabilityNumberOfGames = GetIntFromByteArray(buffer, 242);
                ProbabilityWinPercentage = GetIntFromByteArray(buffer, 246);
                UseBookToHalfMove = GetIntFromByteArray(buffer, 250);

                // Build the index
                BuildIndex(fs);
            }

            FileName = fileName;
            return true;
        } // Open

        public bool SupportGetFromFen()
        {
            return false;
        } // SupportGetFromFen

        public bool SupportGetFromMoves()
        {
            return true;
        } // SupportGetFromMoves

        #region private operations
        private void BuildIndex(FileStream fs)
        {
            if (fs.Seek(MovesStart, SeekOrigin.Begin) != MovesStart)
                return;

            Entry entry;
            while ((entry = ReadEntry(fs)) != null) {
                m_Index[entry.GetMove()] = fs.Position - MoveLength;

                if (entry.NextSiblingPointer <= 0)
                    break;

                // Move to the next sibling
                long fsPos = entry.NextSiblingPointer * MoveLength;
                if (fs.Seek(fsPos, SeekOrigin.Begin) != fsPos)
                    break;
            }
        } // BuildIndex

        private Entry ReadEntry(FileStream fs)
        {
            Entry res = new Entry();

            byte[] buffer = new byte[MoveLength];
            if (fs.Read(buffer, 0, MoveLength) != MoveLength)
                return null;

            char fromFile = (char)('a' + (int)buffer[0] % 8);
            int fromRank = (int)buffer[0] / 8 + 1;
            char toFile = (char)('a' + (int)buffer[1] % 8);
            int toRank = (int)buffer[1] / 8 + 1;
            int promotion = (int)buffer[2];

            res.Move = $"{fromFile}{fromRank}{toFile}{toRank}";
            switch (promotion) {
                case 1:
                    res.Move = $"{res.Move}R";
                    break;
                case 2:
                    res.Move = $"{res.Move}N";
                    break;
                case 3:
                    res.Move = $"{res.Move}B";
                    break;
                case 4:
                    res.Move = $"{res.Move}Q";
                    break;
            }

            res.Priority = (int)buffer[3];
            res.NumberOfGames = GetIntFromByteArray(buffer, 4);
            res.NumberOfWins = GetIntFromByteArray(buffer, 8);
            res.NumberOfLosses = GetIntFromByteArray(buffer, 12);
            res.PlyCount = GetIntFromByteArray(buffer, 16);
            res.NextMovePointer = buffer[20] == 0xFF && buffer[21] == 0xFF && buffer[22] == 0xFF && buffer[23] == 0xFF ? 0 : GetIntFromByteArray(buffer, 20);
            res.NextSiblingPointer = buffer[24] == 0xFF && buffer[25] == 0xFF && buffer[26] == 0xFF && buffer[27] == 0xFF ? 0 : GetIntFromByteArray(buffer, 24);

            return res;
        } // ReadEntry

        private int GetIntFromByteArray(byte[] bytes, int offset)
        {
            return bytes[offset + 3] << 24 | bytes[offset + 2] << 16 | bytes[offset + 1] << 8 | bytes[offset];
        } // GetIntFromByteArray
        #endregion
    }
}
