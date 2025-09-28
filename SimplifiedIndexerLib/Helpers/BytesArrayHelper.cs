using System;
using System.Collections.Generic;

namespace SimplifiedIndexerLib.Helpers
{
    public static class ByteArrayHelper
    {
        public static byte[] ToByteArray(this List<byte[]> bytesList)
        {
            int total = 0;
            foreach (var b in bytesList)
                total += b.Length;

            var combined = new byte[total];
            int offset = 0;
            foreach (var b in bytesList)
            {
                Buffer.BlockCopy(b, 0, combined, offset, b.Length);
                offset += b.Length;
            }
            return combined;
        }
    }
}
