using System.Collections;
using System.Collections.Generic;

namespace VP.Collections.Chunked
{
    public interface IChunkedList<T> : IList<T>, IReadOnlyList<T>, IList
    {
        new T this[int index] { get; set; }

        new void RemoveAt(int index);
    }
}