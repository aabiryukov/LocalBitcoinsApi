using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Permissions;

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
                "Failed request {0}. Message: {1}. Error Code: {2}",
                callerMethod, (string)json.error.message, (int)json.error.error_code)
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

	    protected LocalBitcoinsException(SerializationInfo info, StreamingContext context)
		    : base(info, context)
	    {
	    }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            base.GetObjectData(info, context);

            info.AddValue("RequestMethod", RequestMethod);
        }

    }
}
