using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LocalBitcoins;

namespace LocalbitcoinsApiTest
{
// ReSharper disable once InconsistentNaming
    internal static class LocalbitcoinsTest
    {
        private const string ApiKey = "INSERT-KEY-HERE";
        private const string ApiSecret = "INSERT-SECRET-HERE";

        public static void Test()
        {
            var client = new LocalBitcoinsAPI(ApiKey, ApiSecret);

            dynamic info;

            info = client.GetRecentMessages();
            Console.WriteLine("Res: " + info.ToString());

            info = client.GetDashboardReleased();
            Console.WriteLine("Res: " + info.ToString());

//            info = client.GetContactMessages("7652822");
//            Console.WriteLine("Res: " + info.ToString());

            info = client.GetContactMessageAttachment("6652854", "38026599");
            File.WriteAllBytes(@"c:\temp\LBImage.jpeg", info); // Requires System.IO
            Console.WriteLine("Res: " + info.Length);

            info = client.GetOwnAds();
            Console.WriteLine("Ads: " + info.ToString());

            info = client.GetMyself();
            Console.WriteLine("Myself: " + info.data.username);

            info = client.GetAccountInfo("SomeAccountName");
            Console.WriteLine("User: " + info.data.username);

//            info = client.CheckPinCode("8044011");
//            Console.WriteLine("Pincode: " + info);

            // Update user online
            info = client.GetDashboard();
            Console.WriteLine("Dashboard: " + info.ToString());

            info = client.GetWalletBalance();
            Console.WriteLine("Wallet Balance: " + (decimal)(info.data.total.balance));

            info = client.Logout();
            Console.WriteLine("Logout: " + info.ToString());
        }

    }
}
