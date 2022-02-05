using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChessLib.Books
{
    /// <summary>
    /// OBK opening book (Chessmaster)
    /// </summary>
    public class Obk : IBook
    {
        #region classes
        public class Entry : IBookEntry
        {
            public enum NoteTypes
            {
                None,
                VariationName,
                MoveNotarion,
                EocCode
            }

            public Entry()
            {
                Children = new List<Entry>();
            }

            public string Move { get; set; }
            public bool LastMoveInVariation { get; set; }
            public bool LastMoveInLevel { get; set; }
            public int Weight { get; set; }

            public NoteTypes NotesType { get; set; }
            public string Notes { get; set; }

            public Entry Parent { get; set; }
            public List<Entry> Children { get; set; }

            public string GetMove()
            {
                return Move;
            }

            public int GetPriotity()
            {
                return Weight;
            }            
        } // Entry
        #endregion

        private int m_MoveCount = 0;
        private int m_TextCount = 0;
        private Dictionary<string, List<Entry>> m_Index = null;

        public Obk()
        {

        }

        public string FileName { get; set; }

        public bool Open(string fileName)
        {
            if (!File.Exists(fileName))
                return false;

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                // Read header
                byte[] buffer = new byte[12];
                fs.Read(buffer, 0, 12);

                int type = -1;
                if (buffer[0] == 0x42 && buffer[1] == 0x4F && buffer[2] == 0x4F && buffer[3] == 0x21)
                    type = 0;
                else if (buffer[0] == 0x55 && buffer[1] == 0x47 && buffer[2] == 0x57 && buffer[3] == 0x53) {
                    // No notes
                    fs.Seek(-4, SeekOrigin.Current);
                    type = 1;
                }

                if (type == -1) 
                    return false;

                m_MoveCount = GetIntFromByteArray(buffer, 4);
                m_TextCount = type == 0 ? GetIntFromByteArray(buffer, 8) : 0;

                ReadEntries(fs);
            }

            FileName = fileName;
            return true;
        } // Open

        public void Dispose()
        {

        }

        public List<IBookEntry> GetMovesFromFen(string fenString)
        {
            throw new NotImplementedException();
        } // GetMovesFromFen

        public List<IBookEntry> GetMovesFromMoves(List<string> moves)
        {
            List<IBookEntry> res = null;
            string key = string.Join(" ", moves);
            List<Entry> entries = null;
            if (m_Index.TryGetValue(key, out entries))
                res = entries.OrderByDescending(e => e.Weight).ToList<IBookEntry>();
            return res;
        } // GetMovesFromMoves

        public bool SupportGetFromFen()
        {
            return false;
        } // SupportGetFromFen

        public bool SupportGetFromMoves()
        {
            return true;
        } // SupportGetFromMoves

        #region private operations
        private int GetIntFromByteArray(byte[] bytes, int offset)
        {
            return bytes[offset + 3] << 24 | bytes[offset + 2] << 16 | bytes[offset + 1] << 8 | bytes[offset];
        } // GetIntFromByteArray

        private int GetIntFromBits(BitArray bits, int start, int length)
        {
            int value = 0;
            for (int i = start; i < start + length; i++) {
                if (bits[i])
                    value += Convert.ToInt16(Math.Pow(2, i - start));
            }

            return value;
        } // GetIntFromBits

        private void ReadEntries(FileStream fs)
        {
            m_Index = new Dictionary<string, List<Entry>>();
            var entries = new List<Entry>();
            Entry parent = null;
            List<Entry> branches = new List<Entry>();

            // Read entries
            for (int i = 0; i < m_MoveCount; i++) {
                Entry entry = ReadEntry(fs);
                if (entry == null)
                    break;

                entries.Add(entry);
                if (parent != null) {
                    entry.Parent = parent;
                    parent.Children.Add(entry);
                }

                if (!entry.LastMoveInVariation)
                    parent = entry;
                else {
                    if (branches.Count > 0) {
                        parent = branches[branches.Count - 1].Parent;
                        branches.RemoveAt(branches.Count - 1);
                    } else
                        parent = null;
                }

                if (!entry.LastMoveInLevel)
                    branches.Add(entry);
            }

            // Read notes
            for (int i = 0; i < m_TextCount; i++) {
                if (!ReadEntryNotes(fs, entries))
                    break;
            }

            // Build search index
            foreach (var entry in entries.Where(e => e.Parent == null))
                BuildIndex(entry.Move, entry);
        } // ReadEntries

        private void BuildIndex(string key, Entry entry)
        {
            if (entry.Children.Count == 0)
                return;

            List<Entry> entries;
            if (!m_Index.TryGetValue(key, out entries)) {
                entries = new List<Entry>();
                m_Index[key] = entries;
            }                

            foreach (var child in entry.Children) {
                entries.Add(child);
                BuildIndex($"{key} {child.Move}", child);
            }
        } // BuildIndex

        private Entry ReadEntry(FileStream fs)
        {
            byte[] buffer = new byte[2];

            if (fs.Read(buffer, 0, 2) != 2)
                return null;

            Entry res = new Entry();
            var bits = new BitArray(buffer);

            res.LastMoveInVariation = bits[7];
            res.LastMoveInLevel = bits[6];

            int fromRank = GetIntFromBits(bits, 3, 3) + 1;
            char fromFile = (char)('a' + GetIntFromBits(bits, 0, 3));

            res.Weight = GetIntFromBits(bits, 14, 2);
            switch (res.Weight) {
                case 1:
                    res.Weight = 25;
                    break;
                case 2:
                    res.Weight = 50;
                    break;
                case 3:
                    res.Weight = 100;
                    break;
                default:
                    res.Weight = 0;
                    break;
            }

            int toRank = GetIntFromBits(bits, 11, 3) + 1;
            char toFile = (char)('a' + GetIntFromBits(bits, 8, 3));
            res.Move = $"{fromFile}{fromRank}{toFile}{toRank}";

            if (res.Move == "a1a1")
                return null;

            return res;
        } // ReadEntry

        private bool ReadEntryNotes(FileStream fs, List<Entry> entries)
        {
            byte[] buffer = new byte[6];

            if (fs.Read(buffer, 0, 6) != 6)
                return false;

            int noteIndex = GetIntFromByteArray(buffer, 0);
            int length = buffer[4];
            int type = buffer[5];

            byte[] textBuffer = new byte[length];
            if (fs.Read(textBuffer, 0, textBuffer.Length) != length)
                return false;

            string text = Encoding.UTF8.GetString(textBuffer);

            entries[noteIndex].Notes = text;
            switch (type) {
                case 80:
                    entries[noteIndex].NotesType = Entry.NoteTypes.VariationName;
                    break;
                case 81:
                    entries[noteIndex].NotesType = Entry.NoteTypes.MoveNotarion;
                    break;
                case 82:
                    entries[noteIndex].NotesType = Entry.NoteTypes.EocCode;
                    break;
                default:
                    return false;
            }
            return true;
        } // ReadEntryNotes
        #endregion
    } // Obk
}

