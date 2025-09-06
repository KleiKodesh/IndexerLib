using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexerLib.Helpres
{
    public class ByteArrayComparer : IComparer<byte[]>
    {
        public int Compare(byte[] x, byte[] y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int len = x.Length < y.Length ? x.Length : y.Length;
            for (int i = 0; i < len; i++)
            {
                int diff = x[i] - y[i];
                if (diff != 0)
                    return diff;
            }

            return x.Length.CompareTo(y.Length);
        }
    }
}
