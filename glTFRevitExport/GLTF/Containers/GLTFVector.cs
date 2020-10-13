using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.GLTF.Containers {
    /// <summary>
    /// An integer-based 3D point class
    /// </summary>
    // From Jeremy Tammik's RvtVa3c exporter:
    // https://github.com/va3c/RvtVa3c
    internal class GLTFVector : IComparable<GLTFVector> {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public GLTFVector(float x, float y, float z) {
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
            float d = X - a.X;
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
