using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LocalBitcoins
{
    internal class LocalBitcoinsRestApi
    {
        private const string DefaultApiUrl = "https://localbitcoins.net/";

        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly HttpClient _client;

        private readonly string _accessKey;
        private readonly string _secretKey;

        public LocalBitcoinsRestApi(string accessKey, string secretKey, int apiTimeoutSec, string overrideBaseAddress = null)
        {
            if (overrideBaseAddress == null)
                overrideBaseAddress = DefaultApiUrl;

            _accessKey = accessKey;
            _secretKey = secretKey;

            _client = new HttpClient()
            {
                BaseAddress = new Uri(overrideBaseAddress), // apiv3
                Timeout = TimeSpan.FromSeconds(apiTimeoutSec)
            };
        }

        #region Public Methods

        public async Task<dynamic> CallPublicApiAsync(string requestUri)
        {
            var response = await _client.GetAsync(new Uri(requestUri)).ConfigureAwait(false);
            var resultAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var json = JsonConvert.DeserializeObject<dynamic>(resultAsString);
            return json;
        }

        public async Task<dynamic> CallApiAsync(string apiCommand, RequestType requestType,
            Dictionary<string, string> args, bool getAsBinary = false)
        {
            HttpContent httpContent = null;

            if (requestType == RequestType.Post)
            {
                if (args != null && args.Any())
                {
                    httpContent = new FormUrlEncodedContent(args);
                }
            }

            try
            {
                var nonce = GetNonce();
                var signature = GetSignature(apiCommand, nonce, args);

                var relativeUrl = apiCommand;
                if (requestType == RequestType.Get)
                {
                    if (args != null && args.Any())
                    {
                        relativeUrl += "?" + UrlEncodeParams(args);
                    }
                }

                using (var request = new HttpRequestMessage(
                    requestType == RequestType.Get ? HttpMethod.Get : HttpMethod.Post,
                    new Uri(_client.BaseAddress, relativeUrl)
                    ))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Add("Apiauth-Key", _accessKey);
                    request.Headers.Add("Apiauth-Nonce", nonce);
                    request.Headers.Add("Apiauth-Signature", signature);
                    request.Content = httpContent;

                    var response = await _client.SendAsync(request).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        var resultAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var json = JsonConvert.DeserializeObject<dynamic>(resultAsString);
                        LocalBitcoinsException.ThrowException(apiCommand, json);
                    }

                    if (getAsBinary)
                    {
                        var resultAsByteArray = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        return resultAsByteArray;
                    }
                    else
                    {
                        var resultAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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

        public async Task<dynamic> CallApiPostFileAsync(string apiCommand, Dictionary<string, string> args, string fileName)
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

                var bodyAsBytes = await httpContent.ReadAsByteArrayAsync().ConfigureAwait(false);

                var nonce = GetNonce();
                var signature = GetSignatureBinary(apiCommand, nonce, bodyAsBytes);

                using (var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    new Uri(_client.BaseAddress, apiCommand)
                    ))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Add("Apiauth-Key", _accessKey);
                    request.Headers.Add("Apiauth-Nonce", nonce);
                    request.Headers.Add("Apiauth-Signature", signature);
                    request.Content = httpContent;

                    var response = await _client.SendAsync(request).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        var resultAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var json = JsonConvert.DeserializeObject<dynamic>(resultAsString);
                        LocalBitcoinsException.ThrowException(apiCommand, json);
                    }

                    {
                        var resultAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var json = JsonConvert.DeserializeObject<dynamic>(resultAsString);
                        return json;
                    }
                }
            }
        }

        #endregion Public methods


        #region Private methods

        private static string GetNonce()
        {
            var nonce = (long)((DateTime.UtcNow - _unixEpoch).TotalMilliseconds * 1000);
            return nonce.ToString(CultureInfo.InvariantCulture);
        }

        private string GetSignature(string apiCommand, string nonce, Dictionary<string, string> args)
        {
            return GetSignature(_accessKey, _secretKey, apiCommand, nonce, args);
        }

        private static string GetSignature(string accessKey, string secretKey, string apiCommand, string nonce, Dictionary<string, string> args)
        {
            string paramsStr = null;

            if (args != null && args.Any())
            {
                paramsStr = UrlEncodeParams(args);
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

                var signature = Utility.ByteToString(hmacsha256.ComputeHash(messageByte));
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

        private string GetSignatureBinary(string apiCommand, string nonce, byte[] paramBytes)
        {
            return GetSignatureBinary(_accessKey, _secretKey, apiCommand, nonce, paramBytes);
        }

        private static string GetSignatureBinary(string accessKey, string secretKey, string apiCommand, string nonce, byte[] paramBytes)
        {
            var encoding = new ASCIIEncoding();
            var secretByte = encoding.GetBytes(secretKey);
            using (var hmacsha256 = new HMACSHA256(secretByte))
            {
                var message = nonce + accessKey + apiCommand;
                var messageByte = encoding.GetBytes(message);
                if (paramBytes != null)
                {
                    messageByte = CombineBytes(messageByte, paramBytes);
                }

                var signature = Utility.ByteToString(hmacsha256.ComputeHash(messageByte));
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

        #endregion Private methods
    }
}
