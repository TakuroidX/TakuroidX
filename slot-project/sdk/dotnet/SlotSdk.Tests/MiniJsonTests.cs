using System;
using System.Collections.Generic;
using Xunit;

namespace SlotSdk.Tests
{
    public class MiniJsonTests
    {
        [Fact]
        public void ParsesNestedStructures()
        {
            var o = (Dictionary<string, object>)MiniJson.Parse("{\"a\": [1, 2.5, -3e2], \"b\": {\"c\": \"x\\ny\\u0041\"}, \"d\": true, \"e\": null}");
            var a = (List<object>)o["a"];
            Assert.Equal(new object[] { 1.0, 2.5, -300.0 }, a.ToArray());
            Assert.Equal("x\nyA", ((Dictionary<string, object>)o["b"])["c"]);
            Assert.Equal(true, o["d"]);
            Assert.Null(o["e"]);
        }

        [Theory]
        [InlineData("{")]
        [InlineData("[1,]")]
        [InlineData("{\"a\" 1}")]
        [InlineData("tru")]
        [InlineData("[1] 2")]
        public void RejectsMalformed(string json)
        {
            Assert.Throws<FormatException>(() => MiniJson.Parse(json));
        }
    }
}
