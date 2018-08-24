using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace LocalBitcoins
{
    // ReSharper disable once InconsistentNaming
    public class LocalBitcoinsClient
    {
        private const int DefaultApiTimeoutSec = 10;
        private readonly LocalBitcoinsRestApi _restApi;

        private static readonly JsonSerializer _serializer = JsonSerializer.Create(new JsonSerializerSettings()
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        });

        private class NameValueDictionary : Dictionary<string, string> { }

        /// <summary>
        /// Unique ctor sets access key and secret key, which cannot be changed later.
        /// </summary>
        /// <param name="accessKey">Your Access Key</param>
        /// <param name="secretKey">Your Secret Key</param>
        /// <param name="apiTimeoutSec">API request timeout in seconds</param>
        /// <param name="overrideBaseAddress">Override API base address. Default is "https://localbitcoins.net/"</param>
        public LocalBitcoinsClient(string accessKey, string secretKey, int apiTimeoutSec = DefaultApiTimeoutSec, string overrideBaseAddress = null)
        {
            _restApi = new LocalBitcoinsRestApi(accessKey, secretKey, apiTimeoutSec, overrideBaseAddress);
        }

        protected virtual async Task<dynamic> CallApiAsync(string apiCommand, RequestType requestType = RequestType.Get, Dictionary<string, string> args = null)
        {
            return await _restApi.CallApiAsync(apiCommand).ConfigureAwait(false);
        }

        // Returns public user profile information
        public async Task<dynamic> GetAccountInfo(string userName)
        {
            return await CallApiAsync("/api/account_info/" + userName + "/").ConfigureAwait(false);
        }

        // Return the information of the currently logged in user(the owner of authentication token).
        public async Task<dynamic> GetMyself()
        {
            return await CallApiAsync("/api/myself/").ConfigureAwait(false);
        }

        // Checks the given PIN code against the user"s currently active PIN code.
        // You can use this method to ensure the person using the session is the legitimate user.
        public async Task<dynamic> CheckPinCode(string code)
        {
            var args = new NameValueDictionary
            {
                {"code", code},
            };

            return await CallApiAsync("/api/pincode/", RequestType.Post, args).ConfigureAwait(false);
        }

        // Return open and active contacts
        public async Task<dynamic> GetDashboard()
        {
            return await CallApiAsync("/api/dashboard/").ConfigureAwait(false);
        }

        // Return released(successful) contacts
        public async Task<dynamic> GetDashboardReleased()
        {
            return await CallApiAsync("/api/dashboard/released/").ConfigureAwait(false);
        }

        // Return canceled contacts
        public async Task<dynamic> GetDashboardCanceled()
        {
            return await CallApiAsync("/api/dashboard/canceled/").ConfigureAwait(false);
        }

        // Return closed contacts, both released and canceled
        public async Task<dynamic> GetDashboardClosed()
        {
            return await CallApiAsync("/api/dashboard/closed/").ConfigureAwait(false);
        }

        // Releases the escrow of contact specified by ID { contact_id }.
        // On success there"s a complimentary message on the data key.
        public async Task<dynamic> ContactRelease(string contactId)
        {
            return await CallApiAsync("/api/contact_release/" + contactId + "/", RequestType.Post).ConfigureAwait(false);
        }

        // Releases the escrow of contact specified by ID { contact_id }.
        // On success there"s a complimentary message on the data key.
        public async Task<dynamic> ContactReleasePin(string contactId, string pincode)
        {
            var args = new NameValueDictionary
            {
                {"pincode", pincode},
            };

            return await CallApiAsync("/api/contact_release_pin/" + contactId + "/", RequestType.Post, args).ConfigureAwait(false);
        }

        // Reads all messaging from the contact.Messages are on the message_list key.
        // On success there"s a complimentary message on the data key.
        // attachment_* fields exist only if there is an attachment.
        public async Task<dynamic> GetContactMessages(string contactId)
        {
            return await CallApiAsync("/api/contact_messages/" + contactId + "/").ConfigureAwait(false);
        }

        public async Task<byte[]> GetContactMessageAttachment(string contractId, string attachmentId)
        {
            return await _restApi.CallApiAsync($"/api/contact_message_attachment/{contractId}/{attachmentId}/", RequestType.Get, null, true).ConfigureAwait(false);
        }

        // Marks a contact as paid.
        // It is recommended to access this API through /api/online_buy_contacts/ entries" action key.
        public async Task<dynamic> MarkContactAsPaid(string contactId)
        {
            return await CallApiAsync("/api/contact_mark_as_paid/" + contactId + "/").ConfigureAwait(false);
        }

        // Post a message to contact
        public async Task<dynamic> PostMessageToContact(string contactId, string message, string attachFileName = null)
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

            return await _restApi.CallApiPostFileAsync("/api/contact_message_post/" + contactId + "/", args, attachFileName).ConfigureAwait(false);
        }

        // Starts a dispute with the contact, if possible.
        // You can provide a short description using topic. This helps support to deal with the problem.
        public async Task<dynamic> StartDispute(string contactId, string topic = null)
        {
            NameValueDictionary args = null;
            if (topic != null)
            {
                args = new NameValueDictionary
                {
                    {"topic", topic},
                };
            }

            return await CallApiAsync("/api/contact_dispute/" + contactId + "/", RequestType.Post, args).ConfigureAwait(false);
        }

        // Cancels the contact, if possible
        public async Task<dynamic> CancelContact(string contactId)
        {
            return await CallApiAsync("/api/contact_cancel/" + contactId + "/", RequestType.Post).ConfigureAwait(false);
        }

        // Attempts to fund an unfunded local contact from the seller"s wallet.
        public async Task<dynamic> FundContact(string contactId)
        {
            return await CallApiAsync("/api/contact_fund/" + contactId + "/", RequestType.Post).ConfigureAwait(false);
        }

        // Attempts to create a contact to trade bitcoins.
        // Amount is a number in the advertisement"s fiat currency.
        // Returns the API URL to the newly created contact at actions.contact_url.
        // Whether the contact was able to be funded automatically is indicated at data.funded.
        // Only non-floating LOCAL_SELL may return unfunded, all other trade types either fund or fail.
        public async Task<dynamic> CreateContact(string contactId, decimal amount, string message = null)
        {
            var args = new NameValueDictionary
            {
                {"amount", amount.ToString(CultureInfo.InvariantCulture)},
            };

            if (message != null)
                args.Add("message", message);

            return await CallApiAsync("/api/contact_create/" + contactId + "/", RequestType.Post, args).ConfigureAwait(false);
        }


        // Gets information about a single contact you are involved in. Same fields as in /api/contacts/.
        public async Task<dynamic> GetContactInfo(string contactId)
        {
            return await CallApiAsync("/api/contact_info/" + contactId + "/").ConfigureAwait(false);
        }

        // contacts is a comma-separated list of contact IDs that you want to access in bulk.
        // The token owner needs to be either a buyer or seller in the contacts, contacts that do not pass this check are simply not returned.
        // A maximum of 50 contacts can be requested at a time.
        // The contacts are not returned in any particular order.
        public async Task<dynamic> GetContactsInfo(string contacts)
        {
            var args = new NameValueDictionary
            {
                {"contacts", contacts},
            };
            return await CallApiAsync("/api/contact_info/", RequestType.Get, args).ConfigureAwait(false);
        }

        // Returns maximum of 50 newest trade messages.
        // Messages are ordered by sending time, and the newest one is first.
        // The list has same format as /api/contact_messages/, but each message has also contact_id field.
        public async Task<dynamic> GetRecentMessages()
        {
            return await CallApiAsync("/api/recent_messages/").ConfigureAwait(false);
        }

        // Gives feedback to user.
        // Possible feedback values are: trust, positive, neutral, block, block_without_feedback as strings.
        // You may also set feedback message field with few exceptions. Feedback block_without_feedback clears the message and with block the message is mandatory.
        public async Task<dynamic> PostFeedbackToUser(string userName, string feedback, string message = null)
        {
            var args = new NameValueDictionary
            {
                {"feedback", feedback},
            };

            if (message != null)
                args.Add("msg", message);

            return await CallApiAsync("/api/feedback/" + userName + "/", RequestType.Post, args).ConfigureAwait(false);
        }

        // Gets information about the token owner"s wallet balance.
        public async Task<dynamic> GetWallet()
        {
            return await CallApiAsync("/api/wallet/").ConfigureAwait(false);
        }

        // Same as / api / wallet / above, but only returns the message, receiving_address_list and total fields.
        // (There"s also a receiving_address_count but it is always 1: only the latest receiving address is ever returned by this call.)
        // Use this instead if you don"t care about transactions at the moment.
        public async Task<dynamic> GetWalletBalance()
        {
            return await CallApiAsync("/api/wallet-balance/").ConfigureAwait(false);
        }

        // Sends amount bitcoins from the token owner"s wallet to address.
        // Note that this API requires its own API permission called Money.
        // On success, this API returns just a message indicating success.
        // It is highly recommended to minimize the lifetime of access tokens with the money permission.
        // Call / api / logout / to make the current token expire instantly.
        public async Task<dynamic> WalletSend(decimal amount, string address)
        {
            var args = new NameValueDictionary
            {
                {"amount", amount.ToString(CultureInfo.InvariantCulture)},
                {"address", address},
            };

            return await CallApiAsync("/api/wallet-send/", RequestType.Post, args).ConfigureAwait(false);
        }

        // As above, but needs the token owner"s active PIN code to succeed.
        // Look before you leap. You can check if a PIN code is valid without attempting a send with / api / pincode /.
        // Security concern: To get any security beyond the above API, do not retain the PIN code beyond a reasonable user session, a few minutes at most.
        // If you are planning to save the PIN code anyway, please save some headache and get the real no-pin - required money permission instead.
        public async Task<dynamic> WalletSendWithPin(decimal amount, string address, string pincode)
        {
            var args = new NameValueDictionary
            {
                {"amount", amount.ToString(CultureInfo.InvariantCulture)},
                {"address", address},
                {"pincode", pincode},
            };

            return await CallApiAsync("/api/wallet-send-pin/", RequestType.Post, args).ConfigureAwait(false);
        }

        // Gets an unused receiving address for the token owner"s wallet, its address given in the address key of the response.
        // Note that this API may keep returning the same(unused) address if called repeatedly.
        public async Task<dynamic> GetWalletAddress()
        {
            return await CallApiAsync("/api/wallet-addr/", RequestType.Post).ConfigureAwait(false);
        }

        // Gets the current outgoing and deposit fees in bitcoins (BTC).
        public async Task<dynamic> GetFees()
        {
            return await CallApiAsync("/api/fees/", RequestType.Get).ConfigureAwait(false);
        }

        // Expires the current access token immediately.
        // To get a new token afterwards, public apps will need to reauthenticate, confidential apps can turn in a refresh token.
        public async Task<dynamic> Logout()
        {
            return await CallApiAsync("/api/logout/", RequestType.Post).ConfigureAwait(false);
        }

        // Lists the token owner"s all ads on the data key ad_list, optionally filtered. If there"s a lot of ads, the listing will be paginated.
        // Refer to the ad editing pages for the field meanings.List item structure is like so:
        public async Task<dynamic> GetOwnAds()
        {
            return await CallApiAsync("/api/ads/", RequestType.Get).ConfigureAwait(false);
        }

        // Get ad
        public async Task<dynamic> GetAd(string adId)
        {
            return await CallApiAsync("/api/ad-get/" + adId + "/").ConfigureAwait(false);
        }

        public async Task<dynamic> GetAdList(IEnumerable<string> adList)
        {
            var args = new Dictionary<string, string>
                {
                    {"ads", string.Join(",", adList) },
                };

            return await CallApiAsync("/api/ad-get/", RequestType.Get, args).ConfigureAwait(false);
        }

        // Delete ad
        public async Task<dynamic> DeleteAd(string adId)
        {
            return await CallApiAsync("/api/ad-delete/" + adId + "/", RequestType.Post).ConfigureAwait(false);
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
        public async Task<dynamic> EditAdVisiblity(string adId, bool visible)
        {
            var oldAd = await GetAd(adId).ConfigureAwait(false);
            //            Debug.WriteLine(ad.ToString());

            if ((int)oldAd.data.ad_count < 1)
            {
                throw new LocalBitcoinsException("EditAdVisiblity", "Ad not found. Id=" + adId);
            }

            var args = ParseAdData(oldAd.data.ad_list[0].data);

            args["visible"] = visible ? "1" : "0";

            return await CallApiAsync("/api/ad/" + adId + "/", RequestType.Post, args);
        }

        // Edit ad
        public async Task<dynamic> EditAd(string adId, Dictionary<string, string> values, bool preloadAd = false)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            Dictionary<string, string> args;

            if (preloadAd)
            {
                var oldAd = await GetAd(adId).ConfigureAwait(false);
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

            return await CallApiAsync("/api/ad/" + adId + "/", RequestType.Post, args).ConfigureAwait(false);
        }

        // Edit ad Price equation formula
        public async Task<dynamic> EditAdPriceEquation(string adId, decimal priceEquation)
        {
            if (priceEquation <= 0)
                throw new ArgumentOutOfRangeException(nameof(priceEquation));

            var args = new Dictionary<string, string>()
            {
                ["price_equation"] = priceEquation.ToString(CultureInfo.InvariantCulture)
            };

            return await CallApiAsync("/api/ad-equation/" + adId + "/", RequestType.Post, args).ConfigureAwait(false);
        }

        // Retrieves recent notifications.
        public async Task<dynamic> GetNotifications()
        {
            return await CallApiAsync("/api/notifications/").ConfigureAwait(false);
        }

        // Marks specific notification as read.
        public async Task<dynamic> NotificationMarkAsRead(string notificationId)
        {
            return await CallApiAsync("/api/notifications/mark_as_read/" + notificationId + "/", RequestType.Post).ConfigureAwait(false);
        }

        /// <summary>
        /// This API returns buy Bitcoin online ads. It is closely modeled after the online ad listings on LocalBitcoins.com.
        /// </summary>
        /// <param name="currency">Three letter currency code</param>
        /// <param name="paymentMethod">An example of a valid argument is national-bank-transfer.</param>
        /// <param name="page">Page number, by default 1</param>
        /// <returns>Ads are returned in the same structure as /api/ads/.</returns>
        public async Task<dynamic> PublicMarket_BuyBitcoinsOnlineByCurrency(string currency, string paymentMethod = null, int page = 1)
        {
            var uri = "/buy-bitcoins-online/" + currency + "/";
            if (paymentMethod != null)
                uri += paymentMethod + "/";

            uri += ".json";

            if (page > 1)
                uri += "?page=" + page.ToString(CultureInfo.InvariantCulture);

            return await _restApi.CallPublicApiAsync(uri).ConfigureAwait(false);
        }

        /// <summary>
        /// This API returns sell Bitcoin online ads. It is closely modeled after the online ad listings on LocalBitcoins.com.
        /// </summary>
        /// <param name="currency">Three letter currency code</param>
        /// <param name="paymentMethod">An example of a valid argument is national-bank-transfer.</param>
        /// <param name="page">Page number, by default 1</param>
        /// <returns>Ads are returned in the same structure as /api/ads/.</returns>
        public async Task<dynamic> PublicMarket_SellBitcoinsOnlineByCurrency(string currency, string paymentMethod = null, int page = 1)
        {
            var uri = "/sell-bitcoins-online/" + currency + "/";
            if (paymentMethod != null)
                uri += paymentMethod + "/";

            uri += ".json";

            if (page > 1)
                uri += "?page=" + page.ToString(CultureInfo.InvariantCulture);

            return await _restApi.CallPublicApiAsync(uri).ConfigureAwait(false);
        }
    }
}
