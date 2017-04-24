using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LocalBitcoins
{
    // ReSharper disable once InconsistentNaming
    public sealed class LocalBitcoinsAPI
    {
        private const string DefaultApiUrl = "https://localbitcoins.net/";
        private const int DefaultApiTimeoutSec = 10;

        private enum RequestType
        {
            Get,
            Post
        }

        private class NameValueDictionary : Dictionary<string, string> { }

        private static readonly DateTime st_unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static long GetCurrentUnixTimestampMillis()
        {
            return (long)(DateTime.UtcNow - st_unixEpoch).TotalMilliseconds;
        }

        private static string ByteToString(IEnumerable<byte> buff)
        {
            return buff.Aggregate("", (current, t) => current + t.ToString("X2", CultureInfo.InvariantCulture));
        }

        private string GetSignature(string apiCommand, Dictionary<string, string> args, bool isGetRequest, string nonce)
        {
            return GetSignature(m_accessKey, m_secretKey, apiCommand, args, isGetRequest, nonce);
        }

        public static string GetSignature(string accessKey, string secretKey, string apiCommand, Dictionary<string, string> args, bool isGetRequest, string nonce)
        {
            string paramsStr = null;

            if (args != null && args.Any())
            {
                paramsStr = UrlEncodeParams(args);
                if (isGetRequest)
                    paramsStr = "?" + paramsStr;
            }

            var encoding = new ASCIIEncoding();
            var secretByte = encoding.GetBytes(secretKey);
            using (var hmacsha256 = new HMACSHA256(secretByte))
            {
                var message = nonce + accessKey + apiCommand;
                if (paramsStr != null)
                {
                    message += paramsStr;
                }
                var messageByte = encoding.GetBytes(message);

                var signature = ByteToString(hmacsha256.ComputeHash(messageByte));
                return signature;
            }
        }

        private static byte[] CombineBytes(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        private string GetSignature(string apiCommand, byte[] paramBytes, out string nonce)
        {
            var encoding = new ASCIIEncoding();
            var secretByte = encoding.GetBytes(m_secretKey);
            using (var hmacsha256 = new HMACSHA256(secretByte))
            {
                nonce = GetCurrentUnixTimestampMillis().ToString(CultureInfo.InvariantCulture);
                var message = nonce + m_accessKey + apiCommand;
                var messageByte = encoding.GetBytes(message);
                if (paramBytes != null)
                {
                    messageByte = CombineBytes(messageByte, paramBytes);
                }

                var signature = ByteToString(hmacsha256.ComputeHash(messageByte));
                return signature;
            }
        }

        private static string UrlEncodeString(string text)
        {
            var result = text == null ? "" : Uri.EscapeDataString(text).Replace("%20", "+");
            return result;
        }


        private static string UrlEncodeParams(Dictionary<string, string> args)
        {
            var sb = new StringBuilder();

            var arr =
                args.Select(
                    x =>
                        string.Format(CultureInfo.InvariantCulture, "{0}={1}", UrlEncodeString(x.Key), UrlEncodeString(x.Value))).ToArray();

            sb.Append(string.Join("&", arr));
            return sb.ToString();
        }

        private dynamic CallApi(string apiCommand, RequestType requestType = RequestType.Get,
            Dictionary<string, string> args = null, bool getAsBinary = false)
        {
            HttpContent httpContent = null;

            if (args != null && args.Any())
            {
                httpContent = new FormUrlEncodedContent(args);
            }

            try
            {
                string nonce = GetCurrentUnixTimestampMillis().ToString(CultureInfo.InvariantCulture);
                var signature = GetSignature(apiCommand, args, requestType == RequestType.Get, nonce);

                using (var request = new HttpRequestMessage(
                    requestType == RequestType.Get ? HttpMethod.Get : HttpMethod.Post,
                    new Uri(m_client.BaseAddress, apiCommand)
                    ))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Add("Apiauth-Key", m_accessKey);
                    request.Headers.Add("Apiauth-Nonce", nonce);
                    request.Headers.Add("Apiauth-Signature", signature);
                    request.Content = httpContent;

                    var response = m_client.SendAsync(request).Result;
                    if (!response.IsSuccessStatusCode)
                    {
                        var resultAsString = response.Content.ReadAsStringAsync().Result;
                        var json = JsonConvert.DeserializeObject<dynamic>(resultAsString);
                        throw new LocalBitcoinsException(apiCommand, json);
                    }

                    if (getAsBinary)
                    {
                        var resultAsByteArray = response.Content.ReadAsByteArrayAsync().Result;
                        return resultAsByteArray;
                    }
                    else
                    {
                        var resultAsString = response.Content.ReadAsStringAsync().Result;
                        var json = JsonConvert.DeserializeObject<dynamic>(resultAsString);
                        return json;
                    }
                }
            }
            finally
            {
                httpContent?.Dispose();
            }
        }

        private dynamic CallApiPostFile(string apiCommand, NameValueDictionary args, string fileName)
        {
            using (var httpContent = new MultipartFormDataContent())
            {
                if (args != null)
                {
                    foreach (var keyValuePair in args)
                    {
                        httpContent.Add(new StringContent(keyValuePair.Value),
                            string.Format(CultureInfo.InvariantCulture, "\"{0}\"", keyValuePair.Key));
                    }
                }

                if (fileName != null)
                {
                    var fileBytes = File.ReadAllBytes(fileName);
                    httpContent.Add(new ByteArrayContent(fileBytes), "\"document\"",
                        "\"" + Path.GetFileName(fileName) + "\"");
                }

                var bodyAsBytes = httpContent.ReadAsByteArrayAsync().Result;

                string nonce;
                var signature = GetSignature(apiCommand, bodyAsBytes, out nonce);

                using (var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    new Uri(m_client.BaseAddress + apiCommand)
                    ))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Add("Apiauth-Key", m_accessKey);
                    request.Headers.Add("Apiauth-Nonce", nonce);
                    request.Headers.Add("Apiauth-Signature", signature);
                    request.Content = httpContent;

                    var response = m_client.SendAsync(request).Result;
                    if (!response.IsSuccessStatusCode)
                    {
                        var resultAsString = response.Content.ReadAsStringAsync().Result;
                        var json = JsonConvert.DeserializeObject<dynamic>(resultAsString);
                        throw new LocalBitcoinsException(apiCommand, json);
                    }

                    {
                        var resultAsString = response.Content.ReadAsStringAsync().Result;
                        var json = JsonConvert.DeserializeObject<dynamic>(resultAsString);
                        return json;
                    }
                }
            }
        }

        //		private class NameValueDictionary : SortedDictionary<string, string>{}

        //        private const int ApiTimeoutSeconds = 10;
        //        private readonly string BaseAddress;
        private readonly HttpClient m_client;

        private readonly string m_accessKey;
        private readonly string m_secretKey;
        //constants
        //        private const int BTCChinaConnectionLimit = 1;


        /// <summary>
        /// Unique ctor sets access key and secret key, which cannot be changed later.
        /// </summary>
        /// <param name="accessKey">Your Access Key</param>
        /// <param name="secretKey">Your Secret Key</param>
        public LocalBitcoinsAPI(string accessKey, string secretKey, string baseAddress = DefaultApiUrl, int apiTimeout = DefaultApiTimeoutSec)
        {
            m_accessKey = accessKey;
            m_secretKey = secretKey;

            m_client = new HttpClient(/*new LoggingHandler(new HttpClientHandler())*/)
            {
                BaseAddress = new Uri(baseAddress), // apiv3
                Timeout = TimeSpan.FromSeconds(apiTimeout)
            };

            // HttpWebRequest setups
            //            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; }; //for https
            //            ServicePointManager.DefaultConnectionLimit = 1; //one concurrent connection is allowed for this server atm.
            //ServicePointManager.UseNagleAlgorithm = false;
        }

        // Returns public user profile information
        public dynamic GetAccountInfo(string userName)
        {
            return CallApi("/api/account_info/" + userName + "/");
        }

        // Return the information of the currently logged in user(the owner of authentication token).
        public dynamic GetMyself()
        {
            return CallApi("/api/myself/");
        }

        // Checks the given PIN code against the user"s currently active PIN code.
        // You can use this method to ensure the person using the session is the legitimate user.
        public dynamic CheckPinCode(string code)
        {
            var args = new NameValueDictionary
            {
                {"code", code},
            };

            return CallApi("/api/pincode/", RequestType.Post, args);
        }

        // Return open and active contacts
        public dynamic GetDashboard()
        {
            return CallApi("/api/dashboard/");
        }

        // Return released(successful) contacts
        public dynamic GetDashboardReleased()
        {
            return CallApi("/api/dashboard/released/");
        }

        // Return canceled contacts
        public dynamic GetDashboardCanceled()
        {
            return CallApi("/api/dashboard/canceled/");
        }

        // Return closed contacts, both released and canceled
        public dynamic GetDashboardClosed()
        {
            return CallApi("/api/dashboard/closed/");
        }

        // Releases the escrow of contact specified by ID { contact_id }.
        // On success there"s a complimentary message on the data key.
        public dynamic ContactRelease(string contactId)
        {
            return CallApi("/api/contact_release/" + contactId + "/", RequestType.Post);
        }

        // Releases the escrow of contact specified by ID { contact_id }.
        // On success there"s a complimentary message on the data key.
        public dynamic ContactReleasePin(string contactId, string pincode)
        {
            var args = new NameValueDictionary
            {
                {"pincode", pincode},
            };

            return CallApi("/api/contact_release_pin/" + contactId + "/", RequestType.Post, args);
        }

        // Reads all messaging from the contact.Messages are on the message_list key.
        // On success there"s a complimentary message on the data key.
        // attachment_* fields exist only if there is an attachment.
        public dynamic GetContactMessages(string contactId)
        {
            return CallApi("/api/contact_messages/" + contactId + "/");
        }

        public byte[] GetContactMessageAttachment(string attachmentUrl)
        {
            if (string.IsNullOrEmpty(attachmentUrl))
                throw new ArgumentNullException(nameof(attachmentUrl));

            var index = attachmentUrl.IndexOf("/api/", StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(attachmentUrl));

            return CallApi(attachmentUrl.Remove(0, index), RequestType.Get, null, true);
        }

        // Marks a contact as paid.
        // It is recommended to access this API through /api/online_buy_contacts/ entries" action key.
        public dynamic MarkContactAsPaid(string contactId)
        {
            return CallApi("/api/contact_mark_as_paid/" + contactId + "/");
        }

        /*
                // Post a message to contact
                public dynamic PostMessageToContact(string contactId, string message)
                {
                    var args = new NameValueDictionary
                    {
                        {"msg", message},
                    };
                    return CallApi("/api/contact_message_post/" + contactId + "/", RequestType.Post, args);
                }
        */

        // Post a message to contact
        public dynamic PostMessageToContact(string contactId, string message, string attachFileName = null)
        {
            if (attachFileName != null && !File.Exists(attachFileName))
                throw new LocalBitcoinsException("PostMessageToContact", "File not found: " + attachFileName);

            NameValueDictionary args = null;
            if (!string.IsNullOrEmpty(message))
            {
                args = new NameValueDictionary
                {
                    {"msg", message},
                };
            }

            return CallApiPostFile("/api/contact_message_post/" + contactId + "/", args, attachFileName);
        }

        // Starts a dispute with the contact, if possible.
        // You can provide a short description using topic. This helps support to deal with the problem.
        public dynamic StartDispute(string contactId, string topic = null)
        {
            NameValueDictionary args = null;
            if (topic != null)
            {
                args = new NameValueDictionary
                {
                    {"topic", topic},
                };
            }

            return CallApi("/api/contact_dispute/" + contactId + "/", RequestType.Post, args);
        }

        // Cancels the contact, if possible
        public dynamic CancelContact(string contactId)
        {
            return CallApi("/api/contact_cancel/" + contactId + "/", RequestType.Post);
        }

        // Attempts to fund an unfunded local contact from the seller"s wallet.
        public dynamic FundContact(string contactId)
        {
            return CallApi("/api/contact_fund/" + contactId + "/", RequestType.Post);
        }

        // Attempts to create a contact to trade bitcoins.
        // Amount is a number in the advertisement"s fiat currency.
        // Returns the API URL to the newly created contact at actions.contact_url.
        // Whether the contact was able to be funded automatically is indicated at data.funded.
        // Only non-floating LOCAL_SELL may return unfunded, all other trade types either fund or fail.
        public dynamic CreateContact(string contactId, decimal amount, string message = null)
        {
            var args = new NameValueDictionary
            {
                {"amount", amount.ToString(CultureInfo.InvariantCulture)},
            };

            if (message != null)
                args.Add("message", message);

            return CallApi("/api/contact_create/" + contactId + "/", RequestType.Post, args);
        }


        // Gets information about a single contact you are involved in. Same fields as in /api/contacts/.
        public dynamic GetContactInfo(string contactId)
        {
            return CallApi("/api/contact_info/" + contactId + "/");
        }

        // contacts is a comma-separated list of contact IDs that you want to access in bulk.
        // The token owner needs to be either a buyer or seller in the contacts, contacts that do not pass this check are simply not returned.
        // A maximum of 50 contacts can be requested at a time.
        // The contacts are not returned in any particular order.
        public dynamic GetContactsInfo(string contacts)
        {
            var args = new NameValueDictionary
            {
                {"contacts", contacts},
            };
            return CallApi("/api/contact_info/", RequestType.Get, args);
        }

        // Returns maximum of 50 newest trade messages.
        // Messages are ordered by sending time, and the newest one is first.
        // The list has same format as /api/contact_messages/, but each message has also contact_id field.
        public dynamic GetRecentMessages()
        {
            return CallApi("/api/recent_messages/");
        }

        // Gives feedback to user.
        // Possible feedback values are: trust, positive, neutral, block, block_without_feedback as strings.
        // You may also set feedback message field with few exceptions. Feedback block_without_feedback clears the message and with block the message is mandatory.
        public dynamic PostFeedbackToUser(string userName, string feedback, string message = null)
        {
            var args = new NameValueDictionary
            {
                {"feedback", feedback},
            };

            if (message != null)
                args.Add("msg", message);

            return CallApi("/api/feedback/" + userName + "/", RequestType.Post, args);
        }

        // Gets information about the token owner"s wallet balance.
        public dynamic GetWallet()
        {
            return CallApi("/api/wallet/");
        }

        // Same as / api / wallet / above, but only returns the message, receiving_address_list and total fields.
        // (There"s also a receiving_address_count but it is always 1: only the latest receiving address is ever returned by this call.)
        // Use this instead if you don"t care about transactions at the moment.
        public dynamic GetWalletBalance()
        {
            return CallApi("/api/wallet-balance/");
        }

        // Sends amount bitcoins from the token owner"s wallet to address.
        // Note that this API requires its own API permission called Money.
        // On success, this API returns just a message indicating success.
        // It is highly recommended to minimize the lifetime of access tokens with the money permission.
        // Call / api / logout / to make the current token expire instantly.
        public dynamic WalletSend(decimal amount, string address)
        {
            var args = new NameValueDictionary
            {
                {"amount", amount.ToString(CultureInfo.InvariantCulture)},
                {"address", address},
            };

            return CallApi("/api/wallet-send/", RequestType.Post, args);
        }

        // As above, but needs the token owner"s active PIN code to succeed.
        // Look before you leap. You can check if a PIN code is valid without attempting a send with / api / pincode /.
        // Security concern: To get any security beyond the above API, do not retain the PIN code beyond a reasonable user session, a few minutes at most.
        // If you are planning to save the PIN code anyway, please save some headache and get the real no-pin - required money permission instead.
        public dynamic WalletSendWithPin(decimal amount, string address, string pincode)
        {
            var args = new NameValueDictionary
            {
                {"amount", amount.ToString(CultureInfo.InvariantCulture)},
                {"address", address},
                {"pincode", pincode},
            };

            return CallApi("/api/wallet-send-pin/", RequestType.Post, args);
        }

        // Gets an unused receiving address for the token owner"s wallet, its address given in the address key of the response.
        // Note that this API may keep returning the same(unused) address if called repeatedly.
        public dynamic GetWalletAddress()
        {
            return CallApi("/api/wallet-addr/", RequestType.Post);
        }

        // Expires the current access token immediately.
        // To get a new token afterwards, public apps will need to reauthenticate, confidential apps can turn in a refresh token.
        public dynamic Logout()
        {
            return CallApi("/api/logout/", RequestType.Post);
        }

        // Lists the token owner"s all ads on the data key ad_list, optionally filtered. If there"s a lot of ads, the listing will be paginated.
        // Refer to the ad editing pages for the field meanings.List item structure is like so:
        public dynamic GetOwnAds()
        {
            return CallApi("/api/ads/", RequestType.Post);
        }

        // Get ad
        public dynamic GetAd(string adId)
        {
            return CallApi("/api/ad-get/" + adId + "/");
        }

        // Delete ad
        public dynamic DeleteAd(string adId)
        {
            return CallApi("/api/ad-delete/" + adId + "/", RequestType.Post);
        }

        private static Dictionary<string, string> ParseAdData(dynamic adData)
        {
            var args = new Dictionary<string, string>
                {
                    {"lat", (string)adData.lat},
                    {"price_equation", (string)adData.price_equation},
                    {"lon", (string)adData.lon},
                    {"countrycode", (string)adData.countrycode},
                    {"currency", (string)adData.currency},

                    {"min_amount", (string)adData.min_amount},
                    {"max_amount", (string)adData.max_amount},

                    {"msg", (string)adData.msg},
                    {"require_identification", (string)adData.require_identification},
                    {"sms_verification_required", (string)adData.sms_verification_required},
                    {"require_trusted_by_advertiser", (string)adData.require_trusted_by_advertiser},
                    {"trusted_required", (string)adData.trusted_required},
                    {"track_max_amount", (string)adData.track_max_amount},
                    {"email", (string)adData.email},

                    {"visible", (string)adData.visible},
                };

            if((string)adData.opening_hours != "null")
            {
                args["opening_hours"] = (string)adData.opening_hours;
            }

            if (!string.IsNullOrEmpty((string)adData.limit_to_fiat_amounts))
            {
                args["limit_to_fiat_amounts"] = (string)adData.limit_to_fiat_amounts;
            }

            if (!string.IsNullOrEmpty((string)adData.bank_name))
            {
                args["bank_name"] = (string)adData.bank_name;
            }

            string phone_number = adData.account_details?.phone_number;
            if (adData.account_details?.phone_number != null)
            {
                args["details-phone_number"] = phone_number;
            }

            return args;
        }

        // Edit ad visibility
        public dynamic EditAdVisiblity(string adId, bool visible)
        {
            var oldAd = GetAd(adId);
            //            Debug.WriteLine(ad.ToString());

            if ((int)oldAd.data.ad_count < 1)
            {
                throw new LocalBitcoinsException("EditAdVisiblity", "Ad not found. Id=" + adId);
            }

            var args = ParseAdData(oldAd.data.ad_list[0].data);

            args["visible"] = visible ? "1" : "0";

            return CallApi("/api/ad/" + adId + "/", RequestType.Post, args);
        }

        // Edit ad
        public dynamic EditAd(string adId, Dictionary<string, string> values, bool preloadAd = false)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            Dictionary<string, string> args;

            if (preloadAd)
            {
                var oldAd = GetAd(adId);
                //            Debug.WriteLine(ad.ToString());

                if ((int)oldAd.data.ad_count < 1)
                {
                    throw new LocalBitcoinsException("EditAd", "Ad not found. Id=" + adId);
                }

                args = ParseAdData(oldAd.data.ad_list[0].data);

                // Copy user values
                foreach (var val in values)
                {
                    args[val.Key] = val.Value;
                }
            }
            else
            {
                args = values;
            }

            return CallApi("/api/ad/" + adId + "/", RequestType.Post, args);
        }

        // Retrieves recent notifications.
        public dynamic GetNotifications()
        {
            return CallApi("/api/notifications/");
        }

        // Marks specific notification as read.
        public dynamic NotificationMarkAsRead(string notificationId)
        {
            return CallApi("/api/notifications/mark_as_read/" + notificationId + "/", RequestType.Post);
        }

        private dynamic CallPublicApi(string requestUri)
        {
            var response = m_client.GetAsync(requestUri).Result;
            var resultAsString = response.Content.ReadAsStringAsync().Result;
            var json = JsonConvert.DeserializeObject<dynamic>(resultAsString);
            return json;
        }

        /// <summary>
        /// This API returns buy Bitcoin online ads. It is closely modeled after the online ad listings on LocalBitcoins.com.
        /// </summary>
        /// <param name="currency">Three letter currency code</param>
        /// <param name="paymentMethod">An example of a valid argument is national-bank-transfer.</param>
        /// <param name="page">Page number, by default 1</param>
        /// <returns>Ads are returned in the same structure as /api/ads/.</returns>
        public dynamic PublicMarket_BuyBitcoinsOnlineByCurrency(string currency, string paymentMethod = null, int page = 1)
        {
            var uri = "/buy-bitcoins-online/" + currency + "/";
            if (paymentMethod != null)
                uri += paymentMethod + "/";

            uri += ".json";

            if (page > 1)
                uri += "?page=" + page.ToString(CultureInfo.InvariantCulture);

            return CallPublicApi(uri);
        }

        /// <summary>
        /// This API returns sell Bitcoin online ads. It is closely modeled after the online ad listings on LocalBitcoins.com.
        /// </summary>
        /// <param name="currency">Three letter currency code</param>
        /// <param name="paymentMethod">An example of a valid argument is national-bank-transfer.</param>
        /// <param name="page">Page number, by default 1</param>
        /// <returns>Ads are returned in the same structure as /api/ads/.</returns>
        public dynamic PublicMarket_SellBitcoinsOnlineByCurrency(string currency, string paymentMethod = null, int page = 1)
        {
            var uri = "/sell-bitcoins-online/" + currency + "/";
            if (paymentMethod != null)
                uri += paymentMethod + "/";

            uri += ".json";

            if (page > 1)
                uri += "?page=" + page.ToString(CultureInfo.InvariantCulture);

            return CallPublicApi(uri);
        }
    }
}
