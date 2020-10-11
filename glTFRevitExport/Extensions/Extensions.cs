using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF;

namespace GLTFRevitExport.Extensions {
    internal static class DoubleExtensions {
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
    }

    internal static class RevitExtensions {
        /// <summary>
        /// From Jeremy Tammik's RvtVa3c exporter:
        /// </summary>
        // https://github.com/va3c/RvtVa3c
        // Return an integer value for a Revit Color.
        public static int ToInteger(this Color color)
            => color.Red << 16 | color.Green << 8 | color.Blue;

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

        static public List<string> GetTaxonomies(this Element e) {
            // TODO: add all categories
            var categories = new List<string>();
            if (e.Category != null)
                categories.Add($"revit::{e.Category.Name}");
            return categories;
        }

        static public object GetConvertedValue(this Parameter param) {
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
                    return param.AsDouble().ToMM();

                case StorageType.ElementId:
                    return param.AsElementId().IntegerValue;
            }
            return null;
        }

        /// <summary>
        /// From Jeremy Tammik's RvtVa3c exporter:
        /// https://github.com/va3c/RvtVa3c
        /// Return a dictionary of all the given 
        /// element parameter names and values.
        /// </summary>
        static public Dictionary<string, object>
        GetParamDict(this Element e, List<BuiltInParameter> exclude = null) {
            // private function to find a parameter in a list of builins
            bool ContainsParameter(List<BuiltInParameter> paramList, Parameter param) {
                if (param.Definition is InternalDefinition paramDef)
                    foreach (var paramId in paramList)
                        if (paramDef.Id.IntegerValue == (int)paramId)
                            return true;
                return false;
            }
            // TODO: this needs a formatter for prop name and value
            var paramData = new Dictionary<string, object>();
            foreach (var param in e.GetOrderedParameters()) {
                // exclude requested params (only applies to internal params)
                if (exclude != null && ContainsParameter(exclude, param))
                    continue;

                // otherwise process the parameter value
                // skip useless names
                string paramName = param.Definition.Name;
                // skip useless values
                var paramValue = param.GetConvertedValue();
                if (paramValue is null) continue;
                if (paramValue is int intVal && intVal == -1) continue;

                // add value to dict
                if (!paramData.ContainsKey(paramName))
                    paramData.Add(paramName, paramValue);
            }
            return paramData;
        }

        static public object GetParamValue(this Element e, BuiltInParameter p) {
            if (e.get_Parameter(p) is Parameter param)
                return param.GetConvertedValue();
            return null;
        }

        public static bool IsCategory(this Category c, BuiltInCategory bic)
            => c.Id.IntegerValue == (int)bic;
    }
}
