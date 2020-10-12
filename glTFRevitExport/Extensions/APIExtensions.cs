using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Autodesk.Revit.DB;

using GLTFRevitExport.Containers;

namespace GLTFRevitExport.Extensions {
    internal static class APIExtensions {
        static public string GetId(this Element e) => e?.UniqueId;

        public static string GetId(this Color c)
            => (
            "#"
            + c.Red.ToString("X2")
            + c.Blue.ToString("X2")
            + c.Green.ToString("X2")
            ).ToLower();

        public static bool Compare(this Color left, Color right)
            => left.Red == right.Red
            && left.Blue == right.Blue
            && left.Green == right.Green;

        /// <summary>
        /// From Jeremy Tammik's RvtVa3c exporter:
        /// </summary>
        // https://github.com/va3c/RvtVa3c
        // Return an integer value for a Revit Color.
        public static float[] ToGLTF(this Color color, float transparency) {
            return new float[] {
                color.Red / 255f,
                color.Green / 255f,
                color.Blue / 255f,
                1f - (float)transparency
            };
        }

        /// <summary>
        /// Convert Revit transform to floating-point 4x4 transformation
        /// matrix stored in column major order
        /// </summary>
        static public double[] ToGLTF(this Transform xform) {
            if (xform == null || xform.IsIdentity) return null;

            var bx = xform.BasisX;
            var by = xform.BasisY;
            var bz = xform.BasisZ;
            var or = xform.Origin;

            return new double[16] {
                bx.X,        bx.Y,        bx.Z,        0,
                by.X,        by.Y,        by.Z,        0,
                bz.X,        bz.Y,        bz.Z,        0,
                or.X.ToGLTFLength(), or.Y.ToGLTFLength(), or.Z.ToGLTFLength(), 1
            };
        }

        static public double ToGLTF(this Parameter p, double value) {
            // TODO: read value unit and convert correctly
            switch (p.Definition.UnitType) {
                case UnitType.UT_Length:
                    return value.ToGLTFLength();
                default:
                    return value;
            }
        }

        static public object ToGLTF(this Parameter param) {
            switch (param.StorageType) {
                case StorageType.None: break;

                case StorageType.String:
                    return param.AsString();

                case StorageType.Integer:
                    if (param.Definition.ParameterType == ParameterType.YesNo)
                        return param.AsInteger() != 0;
                    else
                        return param.AsInteger();

                case StorageType.Double:
                    return param.ToGLTF(param.AsDouble());

                case StorageType.ElementId:
                    return param.AsElementId().IntegerValue;
            }
            return null;
        }

        static public GLTFVector ToGLTF(this XYZ p) {
            return new GLTFVector(
                x: p.X.ToGLTFLength(),
                y: p.Y.ToGLTFLength(),
                z: p.Z.ToGLTFLength()
            );
        }

        static public GLTFFace ToGLTF(this PolymeshFacet f) {
            return new GLTFFace(v1: f.V1, v2: f.V2, v3: f.V3);
        }
        
        public static bool IsCategory(this Category c, BuiltInCategory bic)
        => c.Id.IntegerValue == (int)bic;
    }
}
