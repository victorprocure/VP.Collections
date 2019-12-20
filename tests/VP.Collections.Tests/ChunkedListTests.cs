using VP.Collections.Chunked;
using Xunit;

namespace VP.Collections.Tests
{
    public class ChunkedListTests
    {
        [Fact]
        public void GivenChunkedListShouldFunctionAsList()
        {
            var list = new ChunkedList<int>();
            for (var i = 0; i < 10000; i++)
            {
                list.Add(i);
            }

            Assert.True(list.Count == 10000);
            Assert.True(list[10] == 10);
        }

        [Fact]
        public void GivenChunkedListShouldFunctionAsEnumerable()
        {
            var list = new ChunkedList<int>();
            for (var i = 0; i < 5; i++)
            {
                list.Add(i);
            }

            var enumerator = list.GetEnumerator();
            var count = 0;
            var value = 0;
            while (enumerator.MoveNext())
            {
                value = enumerator.Current;
                count++;
            }

            Assert.Equal(5, count);
            Assert.True(value == 4);
        }

        [Fact]
        public void GivenChunkedListOfSizeShouldCreateMultipleListsBucketsToStoreData()
        {
            var list = new ChunkedList<int>(10);
            for (var i = 0; i < 10000; i++)
            {
                list.Add(i);
            }

            Assert.True(list.Count == 10000);
            Assert.True(list[1000] == 1000);
        }

        [Fact]
        public void GivenChunkedListOfSizeShouldCreateMultipleListsBucketsToStoreDataAsEnumerable()
        {
            var list = new ChunkedList<int>(10);
            for (var i = 0; i < 10000; i++)
            {
                list.Add(i);
            }

            var enumerator = list.GetEnumerator();
            var count = 0;
            var value = 0;
            while (enumerator.MoveNext())
            {
                value = enumerator.Current;
                count++;
            }

            Assert.Equal(10000, count);
            Assert.Equal(9999, value);
        }

        [Fact]
        public void GivenBoxedValueShouldAddToList()
        {
            var list = new ChunkedList<int>();

            list.Add(25 as object);

            Assert.True(list.Count == 1);
            Assert.Equal(25, list[0]);
        }

        [Fact]
        public void GivenBoxedValueShouldBeAbleToRemove()
        {
            var list = new ChunkedList<int>
            {
                25
            };

            Assert.True(list.Count == 1);
            Assert.Equal(25, list[0]);

            list.Remove(25 as object);

            Assert.True(list.Count == 0);
        }

        [Fact]
        public void GivenValuesShouldBeAbleToInsert()
        {
            var list = new ChunkedList<int>
            {
                24,
                100,
                84
            };

            list.Insert(3, 19);

            Assert.True(list.Count == 4);
            Assert.Equal(19, list[3]);
        }
    }
}