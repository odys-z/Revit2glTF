using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.Containers {
    /// <summary>
    /// An integer-based 3D point class
    /// </summary>
    // From Jeremy Tammik's RvtVa3c exporter:
    // https://github.com/va3c/RvtVa3c
    internal class GLTFVector : IComparable<GLTFVector> {
        public long X { get; set; }
        public long Y { get; set; }
        public long Z { get; set; }

        public GLTFVector(long x, long y, long z) {
            X = x;
            Y = y;
            Z = z;

            //if (switch_coordinates) {
            //    X = -X;
            //    long tmp = Y;
            //    Y = Z;
            //    Z = tmp;
            //}
        }

        public int CompareTo(GLTFVector a) {
            long d = X - a.X;
            if (0 == d) {
                d = Y - a.Y;
                if (0 == d) {
                    d = Z - a.Z;
                }
            }
            return (0 == d) ? 0 : ((0 < d) ? 1 : -1);
        }
    }

}
