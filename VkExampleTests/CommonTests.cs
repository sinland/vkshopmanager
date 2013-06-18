using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace VkExampleTests
{
    [TestFixture]
    class CommonTests
    {
        [Test]
        public void UnicodeStringDeserializingTest()
        {
            const string responseText = "\u0410\u043D\u0434\u0440\u0435\u0435\u0432";
            var rx = new Regex(@"\\[uU]([0-9A-F]{4})");
            var result = rx.Replace(responseText, match => ((char)Int32.Parse(match.Value.Substring(2), NumberStyles.HexNumber)).ToString());

            Assert.AreEqual("Андреев", result);
        }
        [Test]
        public void UnixTimeConversion()
        {
            const int time = 1203967836;
            throw new ApplicationException(new DateTime(1970, 1, 1).AddSeconds(time).ToString());
        }
    }
}
