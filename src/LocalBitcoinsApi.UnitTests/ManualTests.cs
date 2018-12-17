using System;
using System.IO;
using System.Threading.Tasks;
using LocalBitcoins;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace LocalBitcoinsApi.UnitTests
{
#if (!DEBUG)
    [Ignore]
#endif
    [TestClass]
    public class ManualTests
    {
        [TestMethod]
        public async Task TestRealApiMethods()
        {
            // Read settings from file "TestSettings.json"
            var testSettings = GetSettings();

            var client = new LocalBitcoinsClient(testSettings.ApiKey, testSettings.ApiSecret);

            dynamic info;

            //            info = client.GetContactMessages("7652822");
            //            Console.WriteLine("Res: " + info.ToString());


            info = await client.GetMyself();
            Console.WriteLine("Myself: " + info.data.username);

            info = await client.GetWalletBalance();
            Console.WriteLine("Wallet Balance: " + (decimal)(info.data.total.balance));

            //            info = client.CheckPinCode("8044011");
            //            Console.WriteLine("Pincode: " + info);

            // Update user online
            info = await client.GetDashboard();
            Console.WriteLine("Dashboard: " + info.ToString());

            info = await client.GetOwnAds();
            Console.WriteLine("Ads: " + info.ToString());

            info = await client.GetContactMessageAttachment("6652854", "38026599");
            File.WriteAllBytes(@"c:\temp\LBImage.jpeg", info); // Requires System.IO
            Console.WriteLine("Res: " + info.Length);

            info = await client.GetRecentMessages();
            Console.WriteLine("Res: " + info.ToString());

            info = await client.GetDashboardReleased();
            Console.WriteLine("Res: " + info.ToString());

            info = await client.GetFees();
            Console.WriteLine("Deposit Fee: " + info.data.deposit_fee);

            info = await client.GetAccountInfo("SomeAccountName");
            Console.WriteLine("User: " + info.data.username);

            info = await client.Logout();
            Console.WriteLine("Logout: " + info.ToString());
        }

        private static TestSettings GetSettings()
        {
            const string defaultSettingsFile = "TestSettings.json";
            const string overrideSettingsFile = "TestSettings.override.json";

            var settingsFile = File.Exists(overrideSettingsFile) ? overrideSettingsFile : defaultSettingsFile;
            var settings = JsonConvert.DeserializeObject<TestSettings>(File.ReadAllText(settingsFile));

            if (string.IsNullOrEmpty(settings.ApiKey) || settings.ApiKey == "INSERT-KEY-HERE")
                throw new Exception($"Fill settings in file {settingsFile}");

            return settings;
        }
    }
}
