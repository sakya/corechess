using System;

namespace ChessLib
{
    public abstract class IdObject
    {
        public IdObject()
        {
            Id = Guid.NewGuid().ToString("N");
        }

        public string Id { get; set; }
    }
}