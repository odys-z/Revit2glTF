using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF.Containers;

namespace GLTFRevitExport.Extensions {
    internal static class APIExtensions {
        // Z-Up to Y-Up basis transform
        public static Transform ZTOY =
            Transform.CreateRotation(new XYZ(1, 0, 0), Math.PI / 2.0);


        public static string GetId(this Element e) => e?.UniqueId;

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
                1f - transparency
            };
        }

        public static XYZ ToGLTF(this XYZ vector) => ZTOY.Inverse.OfPoint(vector);

        /// <summary>
        /// Convert Revit transform to floating-point 4x4 transformation
        /// matrix stored in column major order
        /// </summary>
        public static float[] ToGLTF(this Transform xform) {
            if (xform == null || xform.IsIdentity) return null;

            var yupxform = ZTOY.Inverse.Multiply(xform).Multiply(ZTOY);

            var bx = yupxform.BasisX;
            var by = yupxform.BasisY;
            var bz = yupxform.BasisZ;
            var or = yupxform.Origin;

            return new float[16] {
                bx.X.ToSingle(),         bx.Y.ToSingle(),         bx.Z.ToSingle(),         0f,
                by.X.ToSingle(),         by.Y.ToSingle(),         by.Z.ToSingle(),         0f,
                bz.X.ToSingle(),         bz.Y.ToSingle(),         bz.Z.ToSingle(),         0f,
                or.X.ToGLTFLength(),     or.Y.ToGLTFLength(),     or.Z.ToGLTFLength(),     1f
            };
        }

        public static double ToGLTF(this Parameter p, double value) {
            // TODO: read value unit and convert correctly
            switch (p.Definition.UnitType) {
                case UnitType.UT_Length:
                    return value.ToGLTFLength();
                default:
                    return value;
            }
        }

        public static object ToGLTF(this Parameter param) {
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
        
        public static bool IsBIC(this Category c, BuiltInCategory bic)
        => c.Id.IntegerValue == (int)bic;
    }
}
