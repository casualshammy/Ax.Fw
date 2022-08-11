#nullable enable
using Ax.Fw.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests
{
    public class ObjectExtensionsTests
    {
        private readonly ITestOutputHelper p_output;

        public ObjectExtensionsTests(ITestOutputHelper output)
        {
            p_output = output;
        }

        class TestClass
        {
            public TestClass(int _id, string _name)
            {
                Id = _id;
                Name = _name;
            }

            public int Id { get; set; }
            public string Name { get; set; }

            public override bool Equals(object? obj)
            {
                return obj is TestClass @class &&
                       Id == @class.Id &&
                       Name == @class.Name;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id, Name);
            }
        }


        [Fact]
        public async Task GzipTest()
        {
            const int length = 100;
            var rnd = new Random();
            var list = new List<TestClass>();
            for (int i = 0; i < length; i++)
                list.Add(new TestClass(rnd.Next(), rnd.Next().ToString()));

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var compressedList = await Compress.CompressToGzippedJsonAsync(list, cts.Token);
            var decompressedList = await Compress.DecompressGzippedJsonAsync<List<TestClass>>(compressedList, cts.Token);

            Assert.NotNull(decompressedList);

            for (int i = 0; i < length; i++)
            {
                Assert.NotEqual(0, list[i].GetHashCode());
                Assert.Equal(list[i].GetHashCode(), decompressedList![i].GetHashCode());
            }
        }

    }
}
