#nullable enable
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests
{
    public class CompositeKeyTests
    {
        private readonly ITestOutputHelper p_output;

        public CompositeKeyTests(ITestOutputHelper output)
        {
            p_output = output;
        }

        [Fact(Timeout = 30000)]
        public void CtorTest()
        {
            Assert.Equal("abc/def/ghi", new CompositeKey("abc/def/ghi").ToString());
            Assert.Equal("abc", new CompositeKey("/abc").ToString());
            Assert.Equal("abc/ghi", new CompositeKey("/abc/ghi/").ToString());
        }

        [Fact(Timeout = 30000)]
        public void ParentTest()
        {
            var key = new CompositeKey("abc/def/ghi");
            Assert.Equal("abc/def", key.Parent.ToString());
            Assert.Equal("abc", key.Parent.Parent.ToString());
            Assert.Equal("abc", key.Parent.Parent.Parent.ToString());
        }

        [Fact(Timeout = 30000)]
        public void IndexTest()
        {
            var key = new CompositeKey("abc");
            Assert.Equal("abc/def", key["def"].ToString());
            Assert.Equal("abc/def", key["//def//"].ToString());
            Assert.Equal("abc/def/ghi", key["/def/ghi"].ToString());
        }

    }
}
