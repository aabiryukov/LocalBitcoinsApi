using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace LocalBitcoins
{
    [Serializable]
    public class LocalBitcoinsException : Exception
    {
        public string RequestMethod { get; }
        public dynamic DataJson { get; private set; }

        public LocalBitcoinsException()
        { }

        public LocalBitcoinsException(string callerMethod, dynamic json)
            : base(
            string.Format(
                CultureInfo.InvariantCulture,
                "Failed request {0}. Message: {1}. Error Code: {2}. Details: {3}",
                callerMethod, (string)json.error.message, (int)json.error.error_code, (string)json.error.error_lists?.ToString())
                  )
        {
            RequestMethod = callerMethod;
            DataJson = json;
        }
/*
        private static string FormatMessage(string callerMethod, dynamic json)
        {
            return string.Format(
                CultureInfo.InvariantCulture, 
                "Failed request {0}. Message: {1}. ErrorCode: {2}", 
                callerMethod, (string)json.error.message, (int)json.error.error_code);
        }
*/
        public LocalBitcoinsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public LocalBitcoinsException(string message)
            : base(message)
        {
        }

        public LocalBitcoinsException(string callerMethod, string message)
			: base(message)
        {
            RequestMethod = callerMethod;
        }
    }
}
