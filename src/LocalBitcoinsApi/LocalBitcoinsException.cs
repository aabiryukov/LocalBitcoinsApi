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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "dummy")]
        internal LocalBitcoinsException(bool dummy, string callerMethod, dynamic json) //-V3117
            : base(
                json == null 
                ? string.Format( CultureInfo.InvariantCulture, "Failed request {0}. Message: Null", callerMethod)
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "Failed request {0}. Message: {1}. Error Code: {2}. Details: {3}",
                    callerMethod, (string)json.error.message, (int)json.error.error_code, (string)json.error.error_lists?.ToString())
                  )
        {
            RequestMethod = callerMethod;
            DataJson = json;
        }

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
