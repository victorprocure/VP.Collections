using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VP.Collections.Chunked
{
    [DebuggerTypeProxy(typeof(ChunkedListDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public class ChunkedList<T> : IChunkedList<T>
    {
        private const int DefaultSizeOfBucket = 84000;
        private readonly List<List<T>> _buckets;
        private readonly IEqualityComparer<T> _comparer = EqualityComparer<T>.Default;
        private readonly int _maxBucketItems;

        public ChunkedList(IEqualityComparer<T> comparer = null) : this(DefaultSizeOfBucket, comparer)
        {
        }

        public ChunkedList(int defaultSizeOfBucket, IEqualityComparer<T> comparer = null) : this(default, defaultSizeOfBucket, comparer)
        {
        }

        public ChunkedList(int capacity, int defaultSizeOfBucket, IEqualityComparer<T> comparer)
        {
            if (defaultSizeOfBucket <= 0 && defaultSizeOfBucket != default)
                throw new ArgumentOutOfRangeException(nameof(defaultSizeOfBucket));

            if (capacity == default || capacity < 0)
                _buckets = new List<List<T>>();
            else
                _buckets = new List<List<T>>(capacity);

            if (defaultSizeOfBucket == default)
                defaultSizeOfBucket = DefaultSizeOfBucket;

            _maxBucketItems = defaultSizeOfBucket / (typeof(T).IsValueType ? Marshal.SizeOf(typeof(T)) : IntPtr.Size);
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public int Count => _buckets.Count == 0 ? 0 : (_maxBucketItems * (_buckets.Count - 1)) + _buckets[_buckets.Count - 1].Count;

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public bool IsSynchronized => true;

        public object SyncRoot { get; } = new object();

        object IList.this[int index] { get => this[index]; set => this[index] = (T)value; }

        public T this[int index]
        {
            get
            {
                if (index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                try
                {
                    return _buckets[CalculateBucketIndex(index)][CalculateBucketItemIndex(index)];
                }
                catch (ArgumentOutOfRangeException)
                {
                    return this[index + 1];
                }
            }

            set
            {
                if (index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                try
                {
                    _buckets[CalculateBucketIndex(index)][CalculateBucketItemIndex(index)] = value;
                }
                catch (ArgumentOutOfRangeException)
                {
                    this[index + 1] = value;
                }
            }
        }

        public void Add(T item)
        {
            foreach (var bucket in _buckets)
            {
                if (bucket.Count != _maxBucketItems)
                {
                    bucket.Add(item);
                    return;
                }
            }

            _buckets.Add(new List<T>(_maxBucketItems));
            _buckets[_buckets.Count - 1].Add(item);
        }

        public int Add(object value)
        {
            Add((T)value);

            return IndexOf(value);
        }

        public void Insert(int index, T item)
        {
            var list = this.ToList();
            list.Insert(index, item);

            Clear();

            foreach (var listItem in list)
                Add(listItem);
        }

        public void Insert(int index, object value) => Insert(index, (T)value);

        public void Clear()
        {
            _buckets.Clear();
        }

        public bool Remove(T item)
        {
            var removed = false;
            Parallel.ForEach(_buckets, (bucket, state) =>
            {
                var index = bucket.IndexOf(item);
                if (index >= 0)
                {
                    bucket.RemoveAt(index);
                    removed = true;
                    state.Break();
                }
            });

            return removed;
        }

        public void Remove(object value) => Remove((T)value);

        public int IndexOf(object value) => IndexOf((T)value);

        public int IndexOf(T item)
        {
            var bucketIndex = -1;
            var bucketItemIndex = -1;
            Parallel.ForEach(_buckets, (bucket, state) =>
            {
                var index = bucket.IndexOf(item);
                if (index >= 0)
                {
                    bucketIndex = _buckets.IndexOf(bucket);
                    bucketItemIndex = index;
                    state.Break();
                }
            });

            bucketIndex = bucketIndex == 0 ? 0 : bucketIndex - 1;
            return bucketItemIndex == -1 ? -1 : (bucketIndex * _maxBucketItems) + bucketItemIndex;
        }

        public void RemoveAt(int index)
        {
            if (index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            try
            {
                _buckets[CalculateBucketIndex(index)].RemoveAt(CalculateBucketItemIndex(index));
            }
            catch (ArgumentOutOfRangeException)
            {
                RemoveAt(index + 1);
            }
        }

        public bool Contains(T item)
            => Enumerable.Contains(this, item, _comparer);

        public bool Contains(object value) => Contains((T)value);

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count)
                throw new ArgumentOutOfRangeException(nameof(array));

            var index = arrayIndex;
            foreach (var item in this)
            {
                array[index] = item;
                index++;
            }
        }

        public void CopyTo(IList<T> chunkedList)
        {
            if (chunkedList == null)
                throw new ArgumentNullException(nameof(chunkedList));

            foreach (var item in this)
            {
                chunkedList.Add(item);
            }
        }

        public void CopyTo(Array array, int index) => CopyTo((T[])array, index);

        public IEnumerator<T> GetEnumerator()
            => new ChunkedEnumerator<T>(_buckets, _maxBucketItems);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int CalculateBucketIndex(int index) => index / _maxBucketItems;

        private int CalculateBucketItemIndex(int index) => index % _maxBucketItems;
    }
}