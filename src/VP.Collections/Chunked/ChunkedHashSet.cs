using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace VP.Collections.Chunked
{
    public class ChunkedHashSet<T> : IChunkedHashSet<T>
    {
        private const int Lower31BitMask = 0x7FFFFFFF;
        private const int StackAllocThreshold = 100;
        private const int ShrinkThreshold = 3;

        private IEqualityComparer<T> _equalityComparer;
        private ChunkedList<int> _buckets;
        private ChunkedList<Slot> _slots;
        private int _count;
        private int _lastIndex;
        private int _freeList;
        private int _version;

        public int Count => _count;

        public bool IsReadOnly => false;

        public ChunkedHashSet() : this(EqualityComparer<T>.Default)
        {
        }

        public ChunkedHashSet(int capacity): this(capacity, EqualityComparer<T>.Default)
        {
        }

        public ChunkedHashSet(int capacity, IEqualityComparer<T> equalityComparer) : this(equalityComparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            if (capacity > 0)
                Initialize(capacity);
        }

        public ChunkedHashSet(IEqualityComparer<T> equalityComparer)
        {
            _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            _lastIndex = 0;
            _count = 0;
            _freeList = -1;
            _version = 0;
        }

        private void Initialize(int capacity)
        {
            Debug.Assert(_buckets == null, $"Initialize was called but {nameof(_buckets)} was not null");

            var size = HashHelpers.GetPrime(capacity);

            _buckets = new ChunkedList<int>(size, default, null);
            _slots = new ChunkedList<Slot>(size, default, null);
        }

        public bool Add(T item) => AddIfNotPresent(item);
        public void ExceptWith(IEnumerable<T> other) => throw new NotImplementedException();
        public void IntersectWith(IEnumerable<T> other) => throw new NotImplementedException();
        public bool IsProperSubsetOf(IEnumerable<T> other) => throw new NotImplementedException();
        public bool IsProperSupersetOf(IEnumerable<T> other) => throw new NotImplementedException();
        public bool IsSubsetOf(IEnumerable<T> other) => throw new NotImplementedException();
        public bool IsSupersetOf(IEnumerable<T> other) => throw new NotImplementedException();
        public bool Overlaps(IEnumerable<T> other) => throw new NotImplementedException();
        public bool SetEquals(IEnumerable<T> other) => throw new NotImplementedException();
        public void SymmetricExceptWith(IEnumerable<T> other) => throw new NotImplementedException();
        public void UnionWith(IEnumerable<T> other) => throw new NotImplementedException();
        void ICollection<T>.Add(T item) => throw new NotImplementedException();
        public void Clear() => throw new NotImplementedException();
        public bool Contains(T item) => throw new NotImplementedException();
        public void CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();
        public bool Remove(T item) => throw new NotImplementedException();
        public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

        private bool AddIfNotPresent(T value)
        {
            if (_buckets == null)
                Initialize(0);

            var hashCode = InternalGetHashCode(value);
            var bucket = hashCode % _buckets.Count;
            for(var i = _buckets[hashCode % _buckets.Count] -1; i>=0; i = _slots[i].next)
            {
                if (_slots[i].hashCode == hashCode && _equalityComparer.Equals(_slots[i].value, value))
                    return false;
            }

            int index;
            if(_freeList >= 0)
            {
                index = _freeList;
                _freeList = _slots[index].next;
            }
            else
            {
                if(_lastIndex == _slots.Count)
                {
                    IncreaseCapacity();
                    bucket = hashCode % _buckets.Count;
                }
                index = _lastIndex;
                _lastIndex++;
            }

            _slots[index] = new Slot { hashCode = hashCode, next = _buckets[bucket] - 1, value = value };
            _buckets[index] = index + 1;
            _count++;
            _version++;

            return true;
        }

        private void IncreaseCapacity()
        {
            Debug.Assert(_buckets == null, $"{nameof(IncreaseCapacity)} called on a set with no elements");

            var newSize = HashHelpers.ExpandPrime(_count);
            if (newSize <= _count)
                throw new InvalidOperationException("Capacity overflow");

            SetCapacity(newSize, false);
        }

        private void SetCapacity(int newSize, bool forceNewHasCodes)
        {
            if (!HashHelpers.IsPrime(newSize))
                throw new ArgumentException("Size must be a prime", nameof(newSize));
            if (_buckets == null)
                throw new InvalidOperationException($"{nameof(SetCapacity)} called on set with no elements");

            var newSlots = new ChunkedList<Slot>(newSize, default, null);
            if (_slots != null)
                _slots.CopyTo(newSlots);

            if (forceNewHasCodes)
            {
                for(var i = 0; i<_lastIndex; i++)
                {
                    if (newSlots[i].hashCode != -1)
                        newSlots[i] = new Slot { next = newSlots[i].next, value = newSlots[i].value, hashCode = InternalGetHashCode(newSlots[i].value) };
                }
            }

            var newBuckets = new ChunkedList<int>(newSize, default, null);
            for (var i = 0; i < _lastIndex; i++)
            {
                var bucket = newSlots[i].hashCode % newSize;
                newSlots[i] = new Slot { hashCode = newSlots[i].hashCode, value = newSlots[i].value, next = newBuckets[bucket] - 1 };
                newBuckets[bucket] = i + 1;
            }

            _slots = newSlots;
            _buckets = newBuckets;
        }

        private int InternalGetHashCode(T item)
        {
            if (item == default)
                return 0;

            return _equalityComparer.GetHashCode(item) & Lower31BitMask;
        }

        internal struct Slot
        {
            internal int hashCode;
            internal int next;
            internal T value;
        }
    }
}
