using System;
using System.Collections;
using System.Collections.Generic;

namespace VP.Collections.Chunked
{
    internal sealed class ChunkedEnumerator<T> : IEnumerator<T>
    {
        private readonly int _maxBucketItems;
        private readonly List<List<T>> _buckets;
        private int _currentBucket;
        private int _currentIndex;

        public ChunkedEnumerator(List<List<T>> buckets, int maxBucketItems)
        {
            _buckets = buckets;
            _maxBucketItems = maxBucketItems;

            Reset();
        }

        private ChunkedEnumerator(ChunkedEnumerator<T> source)
        {
            _maxBucketItems = source._maxBucketItems;
            _buckets = source._buckets;
            _currentBucket = source._currentBucket;
            _currentIndex = source._currentIndex;
            Current = source.Current;
        }

        public T Current { get; private set; }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            var bucketIndex = _currentIndex / _maxBucketItems;
            var bucketItemIndex = _currentIndex % _maxBucketItems;

            if (!TryValidateBuckets() || (bucketIndex > _buckets.Count - 1))
                return false;
            var bucket = _buckets[bucketIndex];
            if (bucketItemIndex > bucket.Count - 1)
                return false;

            Current = bucket[bucketItemIndex];
            _currentIndex++;

            return true;
        }

        public ChunkedEnumerator<T> Clone()
            => new ChunkedEnumerator<T>(this);

        public void Dispose()
        {
            // Nothing to dispose
        }

        public void Reset()
        {
            _currentBucket = 0;
            _currentIndex = 0;
            Current = default;
        }

        private bool TryValidateBuckets()
            => !(_buckets == null || _buckets.Count == 0 || _buckets[0].Count == 0);
    }
}