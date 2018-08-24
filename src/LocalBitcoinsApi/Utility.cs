using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LocalBitcoins
{
    internal static class Utility
    {
        public static string ByteToString(IEnumerable<byte> buff)
        {
            return buff.Aggregate("", (current, t) => current + t.ToString("X2", CultureInfo.InvariantCulture));
        }
    }
}
