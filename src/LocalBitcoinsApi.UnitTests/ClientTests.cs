using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LocalBitcoins;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace LocalBitcoinsApi.UnitTests
{
    [TestClass]
    public class ClientTests
    {
        private static LocalBitcoinsClient CreateClient()
        {
            return new MockLocalBitcoinsClient();
        }

        [TestMethod]
        public async Task GetMyselfTest()
        {
            var client = CreateClient();

            var result = await client.GetMyself();
            Assert.IsTrue(!string.IsNullOrEmpty((string)result.data.username));
        }
    }
}
