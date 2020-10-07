using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;

namespace GLTFRevitExport.Extensions {
    public static class Extensions {
        /// <summary>
        /// Consider a Revit length zero 
        /// if is smaller than this.
        /// </summary>
        const double _eps = 1.0e-9;

        /// <summary>
        /// Conversion factor from feet to millimetres.
        /// </summary>
        const double _feet_to_mm = 25.4 * 12;

        /// <summary>
        /// Return a string for a real number formatted to two decimal places.
        /// </summary>
        // From Jeremy Tammik's RvtVa3c exporter:
        // https://github.com/va3c/RvtVa3c
        public static string ToFormattedString(this double a)
            => a.ToString("0.##");

        /// <summary>
        /// From Jeremy Tammik's RvtVa3c exporter:
        /// </summary>
        // https://github.com/va3c/RvtVa3c
        // Return an integer value for a Revit Color.
        public static int ToInteger(this Color color)
            => color.Red << 16 | color.Green << 8 | color.Blue;

        /// <summary>
        /// Convert double length value from feet to millimetre
        /// </summary>
        public static long ToMM(this double d) {
            if (0 < d) {
                return _eps > d
                  ? 0
                  : (long)(_feet_to_mm * d + 0.5);
            }
            else {
                return _eps > -d
                  ? 0
                  : (long)(_feet_to_mm * d - 0.5);
            }
        }

        /// <summary>
        /// Convert Revit transform to floating-point 4x4 transformation
        /// matrix stored in column major order
        /// </summary>
        static public double[] ToColumnMajorMatrix(this Transform xform) {
            if (xform == null || xform.IsIdentity) return null;

            var bx = xform.BasisX;
            var by = xform.BasisY;
            var bz = xform.BasisZ;
            var or = xform.Origin;

            return new double[16] {
                bx.X,        bx.Y,        bx.Z,        0,
                by.X,        by.Y,        by.Z,        0,
                bz.X,        bz.Y,        bz.Z,        0,
                or.X.ToMM(), or.Y.ToMM(), or.Z.ToMM(), 1
            };
        }
    
        static public GLTFVector ToGLTF(this XYZ p) {
            return new GLTFVector(x: p.X.ToMM(), y: p.Y.ToMM(), z: p.Z.ToMM());
        }

        static public GLTFFace ToGLTF(this PolymeshFacet f) {
            return new GLTFFace(v1: f.V1, v2: f.V2, v3: f.V3);
        }
    }
}
