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

        public static void ThrowException(string callerMethod, dynamic json)
        {
            var ex = new LocalBitcoinsException(callerMethod, FormatMessage(callerMethod, json))
            {
                DataJson = json
            };

            throw ex;
        }

        private static string FormatMessage(string callerMethod, dynamic json)
        {
            if (json == null)
                return string.Format(CultureInfo.InvariantCulture, "Failed request {0}. Message: Null", callerMethod);

            var result =
                string.Format(CultureInfo.InvariantCulture, "Failed request {0}. Message: {1}. Error Code: {2}.", callerMethod, (string)json.error.message, (int)json.error.error_code);

            if(json.error.error_lists != null)
            {
                result += " Details: " + json.error.error_lists.ToString();
            }

            return result;
        }

        protected LocalBitcoinsException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
    }
}
