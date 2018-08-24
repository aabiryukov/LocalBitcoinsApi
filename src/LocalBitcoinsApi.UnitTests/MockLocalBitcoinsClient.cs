using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LocalBitcoins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LocalBitcoinsApi.UnitTests
{
    internal class MockLocalBitcoinsClient: LocalBitcoinsClient
    {
        public MockLocalBitcoinsClient()
            : base("xxx", "xxx")
        {
        }
        protected override Task<dynamic> CallApiAsync(string apiCommand, RequestType requestType = RequestType.Get, Dictionary<string, string> args = null)
        {
            string resourceName;

            switch (apiCommand.ToLowerInvariant())
            {
                case "/api/myself/":
                    resourceName = "MySelf";
                    break;

                default:
                    throw new ArgumentException($"Unknown api command: {apiCommand}");
            }

            var jobj = JsonConvert.DeserializeObject<dynamic>(GetTestJson(resourceName));
            return Task.FromResult<dynamic>(jobj);
        }

        private static string GetTestJson(string resourceName)
        {
            return File.ReadAllText(Path.Combine("data", resourceName + ".json"));
        }
    }
}
