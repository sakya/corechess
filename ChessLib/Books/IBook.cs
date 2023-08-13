using System;
using System.Collections.Generic;
using System.Text;

namespace ChessLib.Books
{
    public interface IBookEntry
    {
        int GetPriority();
        string GetMove();
    } // IBookEntry

    public interface IBook : IDisposable
    {
        string FileName { get; set; }
        bool Open(string fileName);
        List<IBookEntry> GetMovesFromFen(string fenString);
        bool SupportGetFromFen();
        List<IBookEntry> GetMovesFromMoves(List<string> moves);
        bool SupportGetFromMoves();
    } // IBook

    public static class BookFactory
    {
        public static IBook OpenBook(string fileName)
        {
            IBook res = null;
            string ext = System.IO.Path.GetExtension(fileName);
            if (string.Compare(ext, ".bin", StringComparison.InvariantCultureIgnoreCase) == 0)
                res = new Polyglot();
            else if (string.Compare(ext, ".abk", StringComparison.InvariantCultureIgnoreCase) == 0)
                res = new Abk();
            else if (string.Compare(ext, ".obk", StringComparison.InvariantCultureIgnoreCase) == 0)
                res = new Obk();

            if (res != null) {
                if (res.Open(fileName))
                    return res;
                res.Dispose();
            }
            return null;
        } // OpenBook
    } // BookFactory
}
