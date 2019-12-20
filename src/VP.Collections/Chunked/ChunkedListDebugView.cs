using System.Collections.Generic;
using System.Diagnostics;

namespace VP.Collections.Chunked
{
    internal sealed class ChunkedListDebugView<T>
    {
        private readonly IChunkedList<T> _list;

        public ChunkedListDebugView(IChunkedList<T> list)
        {
            _list = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var items = new T[((ICollection<T>)_list).Count];
                _list.CopyTo(items, 0);
                return items;
            }
        }
    }
}