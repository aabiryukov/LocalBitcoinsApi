using System;
using System.Collections.Generic;

namespace LocalBitcoins
{
    [Obsolete("This class is obsolete. Use instead LocalBitcoinsClient class with async/await.", false)]
    public class LocalBitcoinsAPI
    {
        private const int DefaultApiTimeoutSec = 10;

        private readonly LocalBitcoinsClient _client;

        /// <summary>
        /// Unique ctor sets access key and secret key, which cannot be changed later.
        /// </summary>
        /// <param name="accessKey">Your Access Key</param>
        /// <param name="secretKey">Your Secret Key</param>
        /// <param name="apiTimeoutSec">API request timeout in seconds</param>
        /// <param name="overrideBaseAddress">Override API base address. Default is "https://localbitcoins.net/"</param>
        public LocalBitcoinsAPI(string accessKey, string secretKey, int apiTimeoutSec = DefaultApiTimeoutSec, string overrideBaseAddress = null)
        {
            _client = new LocalBitcoinsClient(accessKey, secretKey, apiTimeoutSec, overrideBaseAddress);
        }

        // Returns public user profile information
        public dynamic GetAccountInfo(string userName) 
            => _client.GetAccountInfo(userName).WaitAndUnwrapException();

        // Return the information of the currently logged in user(the owner of authentication token).
        public dynamic GetMyself() 
            => _client.GetMyself().WaitAndUnwrapException();

        // Checks the given PIN code against the user"s currently active PIN code.
        // You can use this method to ensure the person using the session is the legitimate user.
        public dynamic CheckPinCode(string code) 
            => _client.CheckPinCode(code).WaitAndUnwrapException();

        // Return open and active contacts
        public dynamic GetDashboard() 
            => _client.GetDashboard().WaitAndUnwrapException();

        // Return released(successful) contacts
        public dynamic GetDashboardReleased() 
            => _client.GetDashboardReleased().WaitAndUnwrapException();

        // Return canceled contacts
        public dynamic GetDashboardCanceled() 
            => _client.GetDashboardCanceled().WaitAndUnwrapException();

        // Return closed contacts, both released and canceled
        public dynamic GetDashboardClosed() 
            => _client.GetDashboardClosed().WaitAndUnwrapException();

        // Releases the escrow of contact specified by ID { contact_id }.
        // On success there"s a complimentary message on the data key.
        public dynamic ContactRelease(string contactId) 
            => _client.ContactRelease(contactId).WaitAndUnwrapException();

        // Releases the escrow of contact specified by ID { contact_id }.
        // On success there"s a complimentary message on the data key.
        public dynamic ContactReleasePin(string contactId, string pincode) 
            => _client.ContactReleasePin(contactId, pincode).WaitAndUnwrapException();

        // Reads all messaging from the contact.Messages are on the message_list key.
        // On success there"s a complimentary message on the data key.
        // attachment_* fields exist only if there is an attachment.
        public dynamic GetContactMessages(string contactId) 
            => _client.GetContactMessages(contactId).WaitAndUnwrapException();

        public byte[] GetContactMessageAttachment(string contractId, string attachmentId) 
            => _client.GetContactMessageAttachment(contractId, attachmentId).WaitAndUnwrapException();

        // Marks a contact as paid.
        // It is recommended to access this API through /api/online_buy_contacts/ entries" action key.
        public dynamic MarkContactAsPaid(string contactId) 
            => _client.MarkContactAsPaid(contactId).WaitAndUnwrapException();

        // Post a message to contact
        public dynamic PostMessageToContact(string contactId, string message, string attachFileName = null) 
            => _client.PostMessageToContact(contactId, message, attachFileName).WaitAndUnwrapException();

        // Starts a dispute with the contact, if possible.
        // You can provide a short description using topic. This helps support to deal with the problem.
        public dynamic StartDispute(string contactId, string topic = null) 
            => _client.StartDispute(contactId, topic).WaitAndUnwrapException();

        // Cancels the contact, if possible
        public dynamic CancelContact(string contactId) 
            => _client.CancelContact(contactId).WaitAndUnwrapException();

        // Attempts to fund an unfunded local contact from the seller"s wallet.
        public dynamic FundContact(string contactId) 
            => _client.FundContact(contactId).WaitAndUnwrapException();

        // Attempts to create a contact to trade bitcoins.
        // Amount is a number in the advertisement"s fiat currency.
        // Returns the API URL to the newly created contact at actions.contact_url.
        // Whether the contact was able to be funded automatically is indicated at data.funded.
        // Only non-floating LOCAL_SELL may return unfunded, all other trade types either fund or fail.
        public dynamic CreateContact(string contactId, decimal amount, string message = null) 
            => _client.CreateContact(contactId, amount, message).WaitAndUnwrapException();


        // Gets information about a single contact you are involved in. Same fields as in /api/contacts/.
        public dynamic GetContactInfo(string contactId) 
            => _client.GetContactInfo(contactId).WaitAndUnwrapException();

        // contacts is a comma-separated list of contact IDs that you want to access in bulk.
        // The token owner needs to be either a buyer or seller in the contacts, contacts that do not pass this check are simply not returned.
        // A maximum of 50 contacts can be requested at a time.
        // The contacts are not returned in any particular order.
        public dynamic GetContactsInfo(string contacts) 
            => _client.GetContactsInfo(contacts).WaitAndUnwrapException();

        // Returns maximum of 50 newest trade messages.
        // Messages are ordered by sending time, and the newest one is first.
        // The list has same format as /api/contact_messages/, but each message has also contact_id field.
        public dynamic GetRecentMessages() 
            => _client.GetRecentMessages().WaitAndUnwrapException();

        // Gives feedback to user.
        // Possible feedback values are: trust, positive, neutral, block, block_without_feedback as strings.
        // You may also set feedback message field with few exceptions. Feedback block_without_feedback clears the message and with block the message is mandatory.
        public dynamic PostFeedbackToUser(string userName, string feedback, string message = null) 
            => _client.PostFeedbackToUser(userName, feedback, message).WaitAndUnwrapException();

        // Gets information about the token owner"s wallet balance.
        public dynamic GetWallet() 
            => _client.GetWallet().WaitAndUnwrapException();

        // Same as / api / wallet / above, but only returns the message, receiving_address_list and total fields.
        // (There"s also a receiving_address_count but it is always 1: only the latest receiving address is ever returned by this call.)
        // Use this instead if you don"t care about transactions at the moment.
        public dynamic GetWalletBalance() 
            => _client.GetWalletBalance().WaitAndUnwrapException();

        // Sends amount bitcoins from the token owner"s wallet to address.
        // Note that this API requires its own API permission called Money.
        // On success, this API returns just a message indicating success.
        // It is highly recommended to minimize the lifetime of access tokens with the money permission.
        // Call / api / logout / to make the current token expire instantly.
        public dynamic WalletSend(decimal amount, string address) 
            => _client.WalletSend(amount, address).WaitAndUnwrapException();

        // As above, but needs the token owner"s active PIN code to succeed.
        // Look before you leap. You can check if a PIN code is valid without attempting a send with / api / pincode /.
        // Security concern: To get any security beyond the above API, do not retain the PIN code beyond a reasonable user session, a few minutes at most.
        // If you are planning to save the PIN code anyway, please save some headache and get the real no-pin - required money permission instead.
        public dynamic WalletSendWithPin(decimal amount, string address, string pincode) 
            => _client.WalletSendWithPin(amount, address, pincode).WaitAndUnwrapException();

        // Gets an unused receiving address for the token owner"s wallet, its address given in the address key of the response.
        // Note that this API may keep returning the same(unused) address if called repeatedly.
        public dynamic GetWalletAddress() 
            => _client.GetWalletAddress().WaitAndUnwrapException();

        // Gets the current outgoing and deposit fees in bitcoins (BTC).
        public dynamic GetFees() 
            => _client.GetFees().WaitAndUnwrapException();

        // Expires the current access token immediately.
        // To get a new token afterwards, public apps will need to reauthenticate, confidential apps can turn in a refresh token.
        public dynamic Logout() 
            => _client.Logout().WaitAndUnwrapException();

        // Lists the token owner"s all ads on the data key ad_list, optionally filtered. If there"s a lot of ads, the listing will be paginated.
        // Refer to the ad editing pages for the field meanings.List item structure is like so:
        public dynamic GetOwnAds() 
            => _client.GetOwnAds().WaitAndUnwrapException();

        // Get ad
        public dynamic GetAd(string adId) 
            => _client.GetAd(adId).WaitAndUnwrapException();

        public dynamic GetAdList(IEnumerable<string> adList) 
            => _client.GetAdList(adList).WaitAndUnwrapException();

        // Delete ad
        public dynamic DeleteAd(string adId) 
            => _client.DeleteAd(adId).WaitAndUnwrapException();

        // Edit ad visibility
        public dynamic EditAdVisiblity(string adId, bool visible) 
            => _client.EditAdVisiblity(adId, visible).WaitAndUnwrapException();

        // Edit ad
        public dynamic EditAd(string adId, Dictionary<string, string> values, bool preloadAd = false) 
            => _client.EditAd(adId, values, preloadAd).WaitAndUnwrapException();

        // Edit ad Price equation formula
        public dynamic EditAdPriceEquation(string adId, decimal priceEquation) 
            => _client.EditAdPriceEquation(adId, priceEquation).WaitAndUnwrapException();

        // Retrieves recent notifications.
        public dynamic GetNotifications() 
            => _client.GetNotifications().WaitAndUnwrapException();

        // Marks specific notification as read.
        public dynamic NotificationMarkAsRead(string notificationId) 
            => _client.NotificationMarkAsRead(notificationId).WaitAndUnwrapException();

        /// <summary>
        /// This API returns buy Bitcoin online ads. It is closely modeled after the online ad listings on LocalBitcoins.com.
        /// </summary>
        /// <param name="currency">Three letter currency code</param>
        /// <param name="paymentMethod">An example of a valid argument is national-bank-transfer.</param>
        /// <param name="page">Page number, by default 1</param>
        /// <returns>Ads are returned in the same structure as /api/ads/.</returns>
        public dynamic PublicMarket_BuyBitcoinsOnlineByCurrency(string currency, string paymentMethod = null, int page = 1) 
            => _client.PublicMarket_BuyBitcoinsOnlineByCurrency(currency, paymentMethod, page).WaitAndUnwrapException();

        /// <summary>
        /// This API returns sell Bitcoin online ads. It is closely modeled after the online ad listings on LocalBitcoins.com.
        /// </summary>
        /// <param name="currency">Three letter currency code</param>
        /// <param name="paymentMethod">An example of a valid argument is national-bank-transfer.</param>
        /// <param name="page">Page number, by default 1</param>
        /// <returns>Ads are returned in the same structure as /api/ads/.</returns>
        public dynamic PublicMarket_SellBitcoinsOnlineByCurrency(string currency, string paymentMethod = null, int page = 1) 
            => _client.PublicMarket_SellBitcoinsOnlineByCurrency(currency, paymentMethod, page).WaitAndUnwrapException();
    }
}
